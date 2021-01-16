using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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
            var result = new List<byte>();
            var parameters = new List<ParameterExpression>(parameterExpressions);
            serialize(expression, parameters, result);
            return result.ToArray();
        }

        private void serialize(Expression expression, List<ParameterExpression> parameters, List<byte> result)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                result.Add((byte)expression.NodeType);
                serialize(binaryExpression.Left, parameters, result);
                serialize(binaryExpression.Right, parameters, result);
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                result.Add((byte)expression.NodeType);
                serialize(unaryExpression.Operand, parameters, result);

                switch (expression.NodeType)
                {
                    case ExpressionType.Unbox:
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

                for (var i = 0; i < parameters.Count; i++)
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
                addInt16(result, (short)lambdaExpression.Parameters.Count);
                for (var i = 0; i < lambdaExpression.Parameters.Count; i++)
                {
                    if (lambdaExpression.Parameters[i].IsByRef)
                        throw new NotSupportedException("ByRef types are not supported");

                    addType(result, lambdaExpression.Parameters[i].Type);
                    addString(result, lambdaExpression.Parameters[i].Name);

                    parameters.Add(lambdaExpression.Parameters[i]);
                }

                serialize(lambdaExpression.Body, parameters, result);

                parameters.RemoveRange(oldParametersCount, parameters.Count - oldParametersCount);
            }
            else if (expression is ConstantExpression constantExpression)
            {
                result.Add((byte)expression.NodeType);

                var value = constantExpression.Value;
                var type = value?.GetType() ?? typeof(object);

                if (type != constantExpression.Type && value != null) // boxied
                    type = typeof(Nullable<>).MakeGenericType(type);

                addType(result, type);
                addValue(result, constantExpression.Value, type);
            }
            else if (expression is MethodCallExpression callExpression)
            {
                result.Add((byte)expression.NodeType);

                if (callExpression.Object == null)
                    serialize(Expression.Constant(null, typeof(object)), parameters, result);
                else
                    serialize(callExpression.Object, parameters, result);

                addType(result, callExpression.Method.DeclaringType);
                addString(result, callExpression.Method.Name);

                addInt16(result, (short)callExpression.Arguments.Count);
                for (var i = 0; i < callExpression.Arguments.Count; i++)
                    serialize(callExpression.Arguments[i], parameters, result);
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
            else throw new NotSupportedException(expression.NodeType.ToString());
        }

        private Expression alterMemberExpression(MemberExpression expression, List<ParameterExpression> parameters)
        {
            var src = expression.Expression;
            if (src is MemberExpression memberExpressionSrc)
            {
                src = alterMemberExpression(memberExpressionSrc, parameters);
                if (src != null)
                    expression = Expression.MakeMemberAccess(src, expression.Member);
                else
                    src = expression.Expression;
            }

            if (src.NodeType == ExpressionType.Constant)
                return Expression.Constant(_expressionEvaluator.Eval(expression));

            return null;
        }

        private void addValue(List<byte> result, object value, Type type)
        {
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                var array = (Array)value;
                var len = array.Length;

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
                    addString(result, value.ToString());
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

                    throw new NotSupportedException(typeCode.ToString());
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

        private short getInt16(byte[] data, ref int index)
        {
            return (short)(data[index++] | (data[index++] << 8));
        }

        private static void addInt32(List<byte> result, int value)
        {
            result.Add((byte)value);
            result.Add((byte)(value >> 8));
            result.Add((byte)(value >> 16));
            result.Add((byte)(value >> 24));
        }

        private int getInt32(byte[] data, ref int index)
        {
            var lo = (int)(ushort)getInt16(data, ref index);
            var hi = (ushort)getInt16(data, ref index);
            return lo | (hi << 16);
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
                    typeId = AdditionalTypeCodes._ExternalTypeCode;
                    result.Add((byte)typeId);
                    addInt32(result, (int)_types.GetId(type));
                    return;
                }
            }

            result.Add((byte)typeId);
        }

        private void addString(List<byte> result, string name)
        {
            addInt16(result, (short)name.Length);
            for (var i = 0; i < name.Length; i++)
            {
                addInt16(result, (short)name[i]);
            }
        }

        private unsafe string getString(byte[] data, ref int index)
        {
            var len = getInt16(data, ref index);
            var buffer = stackalloc char[len];
            for (var i = 0; i < len; i++)
                buffer[i] = (char)getInt16(data, ref index);

            return new string(buffer, 0, len);
        }
    }
}
