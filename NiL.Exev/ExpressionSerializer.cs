using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace NiL.Exev
{
    public class ExpressionSerializer
    {
        private readonly TypesMapLayer _types;
        private readonly ExpressionEvaluator _expressionEvaluator;

        public ExpressionSerializer()
            : this(new TypesMapLayer())
        {
        }

        public ExpressionSerializer(TypesMapLayer types)
            : this(types, new ExpressionEvaluator())
        {
        }

        public ExpressionSerializer(TypesMapLayer types, ExpressionEvaluator expressionEvaluator)
        {
            _types = types ?? throw new ArgumentNullException(nameof(types));
            _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        }

        public byte[] Serialize(Expression expression, params ParameterExpression[] parameterExpressions)
        {
            var result = new List<byte>(128);
            var parameters = new List<ParameterExpression>(parameterExpressions);
            serialize(expression, parameters, result);
            return result.ToArray();
        }

        private void serialize(Expression expression, List<ParameterExpression> parameters, List<byte> result)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                if (binaryExpression.Method != null
                    && expression.NodeType != ExpressionType.Power
                    && expression.NodeType != ExpressionType.PowerAssign)
                {
                    addMethodCall(parameters, result, new[] { binaryExpression.Left, binaryExpression.Right }, binaryExpression.Method, null);
                }
                else
                {
                    result.Add((byte)expression.NodeType);
                    serialize(binaryExpression.Left, parameters, result);
                    serialize(binaryExpression.Right, parameters, result);
                }
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                result.Add((byte)expression.NodeType);
                serialize(unaryExpression.Operand, parameters, result);

                switch (expression.NodeType)
                {
                    case ExpressionType.Unbox:
                    case ExpressionType.TypeAs:
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        addType(result, unaryExpression.Type);
                        break;

                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.Not:
                    case ExpressionType.Quote:
                    case ExpressionType.IsTrue:
                    case ExpressionType.IsFalse:
                    case ExpressionType.PostDecrementAssign:
                    case ExpressionType.PostIncrementAssign:
                    case ExpressionType.PreDecrementAssign:
                    case ExpressionType.PreIncrementAssign:
                    case ExpressionType.OnesComplement:
                    case ExpressionType.ArrayLength: break;

                    default: throw new NotImplementedException(expression.NodeType.ToString());
                }
            }
            else if (expression is MemberExpression memberExpression)
            {
                var alterExpression = alterMemberExpression(memberExpression, parameters);
                if (alterExpression == null)
                {
                    result.Add((byte)expression.NodeType);

                    serialize(memberExpression.Expression, parameters, result);
                    addType(result, memberExpression.Member.DeclaringType);
                    addString(result, memberExpression.Member.Name);
                }
                else
                {
                    serialize(alterExpression, parameters, result);
                }
            }
            else if (expression is ParameterExpression parameter)
            {
                result.Add((byte)expression.NodeType);

                for (var i = parameters.Count; i-- > 0;)
                {
                    if (parameters[i] == parameter)
                    {
                        addInt16(result, (short)i);
                        return;
                    }
                }

                throw new InvalidOperationException("Unknown parameter " + parameter);
            }
            else if (expression is LambdaExpression lambdaExpression)
            {
                result.Add((byte)expression.NodeType);

                var oldParametersCount = parameters.Count;
                serializeVariables(parameters, result, lambdaExpression.Parameters);

                serialize(lambdaExpression.Body, parameters, result);

                parameters.RemoveRange(oldParametersCount, parameters.Count - oldParametersCount);
            }
            else if (expression is ConstantExpression constantExpression)
            {
                result.Add((byte)expression.NodeType);

                var value = constantExpression.Value;
                var type = constantExpression.Type;

                addType(result, type);
                addValue(result, value, type);
            }
            else if (expression is MethodCallExpression callExpression)
            {
                var arguments = callExpression.Arguments;
                var method = callExpression.Method;
                var target = callExpression.Object;

                addMethodCall(parameters, result, arguments, method, target);
            }
            else if (expression is InvocationExpression invocationExpression)
            {
                result.Add((byte)expression.NodeType);

                serialize(invocationExpression.Expression, parameters, result);
                addInt16(result, (short)invocationExpression.Arguments.Count);
                for (var i = 0; i < invocationExpression.Arguments.Count; i++)
                    serialize(invocationExpression.Arguments[i], parameters, result);
            }
            else if (expression is ConditionalExpression conditionalExpression)
            {
                result.Add((byte)expression.NodeType);

                addType(result, conditionalExpression.Type);
                serialize(conditionalExpression.Test, parameters, result);
                serialize(conditionalExpression.IfTrue, parameters, result);
                serialize(conditionalExpression.IfFalse, parameters, result);
            }
            else if (expression is NewArrayExpression newArrayExpression)
            {
                result.Add((byte)expression.NodeType);

                addType(result, newArrayExpression.Type.GetElementType());
                addInt32(result, newArrayExpression.Expressions.Count);
                for (var i = 0; i < newArrayExpression.Expressions.Count; i++)
                    serialize(newArrayExpression.Expressions[i], parameters, result);
            }
            else if (expression is IndexExpression indexExpression)
            {
                result.Add((byte)expression.NodeType);

                serialize(indexExpression.Object, parameters, result);
                result.Add((byte)indexExpression.Arguments.Count);
                for (var i = 0; i < indexExpression.Arguments.Count; i++)
                    serialize(indexExpression.Arguments[i], parameters, result);
            }
            else if (expression is BlockExpression blockExpression)
            {
                result.Add((byte)expression.NodeType);

                addInt32(result, blockExpression.Expressions.Count);

                var oldParametersCount = parameters.Count;
                serializeVariables(parameters, result, blockExpression.Variables);

                for (var i = 0; i < blockExpression.Expressions.Count; i++)
                    serialize(blockExpression.Expressions[i], parameters, result);

                parameters.RemoveRange(oldParametersCount, parameters.Count - oldParametersCount);
            }
            else if (expression is NewExpression newExpression)
            {
                result.Add((byte)expression.NodeType);

                if (newExpression.Members != null && newExpression.Members.Count != 0)
                    throw new NotImplementedException("newExpression.Members is not implemented");

                addType(result, newExpression.Type);
                addInt16(result, (short)newExpression.Arguments.Count);
                for (var i = 0; i < newExpression.Arguments.Count; i++)
                    serialize(newExpression.Arguments[i], parameters, result);
            }
            else throw new NotSupportedException(expression.NodeType.ToString());
        }

        private void addMethodCall(List<ParameterExpression> parameters, List<byte> result, IList<Expression> arguments, System.Reflection.MethodInfo method, Expression target)
        {
            result.Add((byte)ExpressionType.Call);

            if (target == null)
                serialize(Expression.Constant(null, typeof(object)), parameters, result);
            else
                serialize(target, parameters, result);

            addType(result, method.DeclaringType);
            addString(result, method.Name);

            addInt16(result, (short)arguments.Count);
            for (var i = 0; i < arguments.Count; i++)
                serialize(arguments[i], parameters, result);

            result.Add((byte)(method.IsConstructedGenericMethod ? 1 : 0));

            if (method.IsConstructedGenericMethod)
            {
                var genericArguments = method.GetGenericArguments();

                result.Add(checked((byte)genericArguments.Length));

                for (var i = 0; i < genericArguments.Length; i++)
                {
                    addType(result, genericArguments[i]);
                }
            }
        }

        private void serializeVariables(List<ParameterExpression> currentVariables, List<byte> result, IList<ParameterExpression> newVariables)
        {
            addInt16(result, (short)newVariables.Count);
            for (var i = 0; i < newVariables.Count; i++)
            {
                if (newVariables[i].IsByRef)
                    throw new NotSupportedException("ByRef types are not supported");

                addType(result, newVariables[i].Type);
                addString(result, newVariables[i].Name);

                currentVariables.Add(newVariables[i]);
            }
        }

        private Expression alterMemberExpression(MemberExpression expression, List<ParameterExpression> parameters)
        {
            var source = expression.Expression;
            if (source is MemberExpression memberExpressionSrc)
            {
                source = alterMemberExpression(memberExpressionSrc, parameters);
                if (source != null)
                    expression = Expression.MakeMemberAccess(source, expression.Member);
                else
                    source = expression.Expression;
            }

            if (source == null) // static field
                return Expression.Constant(_expressionEvaluator.Eval(expression), expression.Type);
            else if (source.NodeType == ExpressionType.Constant)
                return Expression.Constant(_expressionEvaluator.Eval(expression), expression.Type);

            return null;
        }

        private void addValue(List<byte> result, object value, Type type)
        {
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                var array = (Array)value;
                var len = array == null ? -1 : array.Length;

                addInt32(result, len);

                for (var i = 0; i < len; i++)
                    addValue(result, array.GetValue(i), itemType);

                return;
            }

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Byte:
                    result.Add((byte)value);
                    break;

                case TypeCode.SByte:
                    result.Add((byte)(sbyte)value);
                    break;

                case TypeCode.Char:
                    addInt16(result, (short)(char)value);
                    break;

                case TypeCode.Int16:
                    addInt16(result, (short)value);
                    break;

                case TypeCode.UInt16:
                    addInt16(result, (short)(ushort)value);
                    break;

                case TypeCode.Int32:
                    addInt32(result, (int)value);
                    break;

                case TypeCode.UInt32:
                    addInt32(result, (int)(uint)value);
                    break;

                case TypeCode.Int64:
                    addInt64(result, (long)value);
                    break;

                case TypeCode.UInt64:
                    addInt64(result, (long)(ulong)value);
                    break;

                case TypeCode.Single:
                    unsafe
                    {
                        var f = (float)value;
                        addInt32(result, *(int*)&f);
                        break;
                    }

                case TypeCode.Double:
                    unsafe
                    {
                        var d = (double)value;
                        addInt64(result, *(long*)&d);
                        break;
                    }

                case TypeCode.Boolean:
                    result.Add((bool)value ? (byte)1 : (byte)0);
                    break;

                case TypeCode.String:
                    addString(result, value?.ToString());
                    break;

                case TypeCode.DateTime:
                    addInt64(result, ((DateTime)value).ToBinary());
                    break;

                case TypeCode.Decimal:
                    unsafe
                    {
                        var d = (decimal)value;
                        var p = (byte*)&d;
                        for (var i = 0; i < sizeof(decimal); i++)
                            result.Add(*p++);
                        break;
                    }

                case TypeCode.Object:
                {
                    if (value == null)
                    {
                        result.Add(0);
                        break;
                    }

                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        addValue(result, value, type.GetGenericArguments()[0]);
                        break;
                    }

                    if (value is Type valueAsType)
                    {
                        addString(result, valueAsType.AssemblyQualifiedName);
                        break;
                    }

                    Type valueType = value.GetType();

                    if (type == typeof(object))
                        addType(result, valueType);

                    if (valueType.IsPrimitive
                        || valueType.IsArray
                        || value is string)
                    {
                        addValue(result, value, valueType);
                        break;
                    }

                    if (valueType.IsValueType)
                    {
                        var members = _MetadataWrappersCache.GetMembers(valueType);
                        for (var i = 0; i < members.Length; i++)
                        {
                            if (members[i] is FieldInfo field)
                            {
                                addValue(result, field.GetValue(value), field.FieldType);
                            }
                        }

                        break;
                    }

                    throw new NotSupportedException(valueType.ToString() + " is not supported for serialization");
                }

                case TypeCode.Empty:
                case TypeCode.DBNull:
                    throw new NotSupportedException(typeCode.ToString());

                default: throw new NotImplementedException(type.ToString());
            }
        }

        private void addInt16(List<byte> result, short value)
        {
            result.Add((byte)value);
            result.Add((byte)(value >> 8));
        }

        private static void addInt32(List<byte> result, int value)
        {
            result.Add((byte)value);
            result.Add((byte)(value >> 8));
            result.Add((byte)(value >> 16));
            result.Add((byte)(value >> 24));
        }

        private static void addInt64(List<byte> result, long value)
        {
            var lo = (int)value;
            var hi = (int)(value >> 32);
            addInt32(result, lo);
            addInt32(result, hi);
        }

        private void addType(List<byte> result, Type type)
        {
            if (type.IsArray)
            {
                result.Add((byte)AdditionalTypeCodes._ArrayTypeCode);
                addType(result, type.GetElementType());
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                result.Add((byte)AdditionalTypeCodes._BoxiedTypeCode);
                addType(result, type.GetGenericArguments()[0]);
                return;
            }

            var typeId = Type.GetTypeCode(type);

            if (typeId == TypeCode.Object)
            {
                if (type != typeof(object))
                {
                    if (_types.TryGetId(type, out var id))
                    {
                        typeId = AdditionalTypeCodes._RegisteredTypeCode;
                        result.Add((byte)typeId);
                        addInt32(result, (int)id);
                    }
                    else
                    {
                        typeId = AdditionalTypeCodes._UnregisteredTypeCode;
                        result.Add((byte)typeId);
                        addString(result, type.FullName);
                    }

                    return;
                }
            }

            result.Add((byte)typeId);
        }

        private void addString(List<byte> result, string value)
        {
            if (value == null)
                addInt16(result, -1);

            if (value.Length > short.MaxValue)
                throw new ArgumentOutOfRangeException("String is too long");

            addInt16(result, (short)value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                addInt16(result, (short)value[i]);
            }
        }
    }
}
