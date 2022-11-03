using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NiL.Exev.FakeTypes;

namespace NiL.Exev
{
    public class ExpressionDeserializer
    {
        private readonly Dictionary<string, Type> _loadedType = new Dictionary<string, Type>();
        private readonly TypesMapLayer _types;

        public ExpressionDeserializer()
            : this(new TypesMapLayer())
        {
        }

        public ExpressionDeserializer(TypesMapLayer types)
        {
            _types = types ?? throw new ArgumentNullException(nameof(types));
        }

        public Expression Deserialize(byte[] data, params ParameterExpression[] parameters)
        {
            var prms = new List<ParameterExpression>(parameters);
            var index = 0;
            return deserialize(data, ref index, prms);
        }

        private Expression deserialize(byte[] data, ref int index, List<ParameterExpression> parameters)
        {
            var nodeType = (ExpressionType)data[index++];

            switch (nodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Modulo:
                case ExpressionType.ModuloAssign:
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.Coalesce:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.AndAssign:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.OrAssign:
                case ExpressionType.Assign:
                case ExpressionType.LeftShift:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.RightShift:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.NotEqual:
                case ExpressionType.Power:
                case ExpressionType.PowerAssign:
                    {
                        var left = deserialize(data, ref index, parameters);
                        var right = deserialize(data, ref index, parameters);

                        switch (nodeType)
                        {
                            case ExpressionType.Add: return Expression.Add(left, right);
                            case ExpressionType.AddChecked: return Expression.AddChecked(left, right);
                            case ExpressionType.AddAssign: return Expression.AddAssign(left, right);
                            case ExpressionType.AddAssignChecked: return Expression.AddAssign(left, right);
                            case ExpressionType.Multiply: return Expression.Multiply(left, right);
                            case ExpressionType.MultiplyAssign: return Expression.MultiplyAssign(left, right);
                            case ExpressionType.MultiplyChecked: return Expression.MultiplyChecked(left, right);
                            case ExpressionType.MultiplyAssignChecked: return Expression.MultiplyAssignChecked(left, right);
                            case ExpressionType.Modulo: return Expression.Modulo(left, right);
                            case ExpressionType.ModuloAssign: return Expression.ModuloAssign(left, right);
                            case ExpressionType.Divide: return Expression.Divide(left, right);
                            case ExpressionType.DivideAssign: return Expression.DivideAssign(left, right);
                            case ExpressionType.Equal: return Expression.Equal(left, right);
                            case ExpressionType.GreaterThan: return Expression.GreaterThan(left, right);
                            case ExpressionType.GreaterThanOrEqual: return Expression.GreaterThanOrEqual(left, right);
                            case ExpressionType.LessThan: return Expression.LessThan(left, right);
                            case ExpressionType.LessThanOrEqual: return Expression.LessThanOrEqual(left, right);
                            case ExpressionType.ExclusiveOr: return Expression.ExclusiveOr(left, right);
                            case ExpressionType.ExclusiveOrAssign: return Expression.ExclusiveOrAssign(left, right);
                            case ExpressionType.Coalesce: return Expression.Coalesce(left, right);
                            case ExpressionType.And: return Expression.And(left, right);
                            case ExpressionType.AndAlso: return Expression.AndAlso(left, right);
                            case ExpressionType.AndAssign: return Expression.AndAssign(left, right);
                            case ExpressionType.Or: return Expression.And(left, right);
                            case ExpressionType.OrElse: return Expression.OrElse(left, right);
                            case ExpressionType.OrAssign: return Expression.OrAssign(left, right);
                            case ExpressionType.Assign: return Expression.Assign(left, right);
                            case ExpressionType.LeftShift: return Expression.LeftShift(left, right);
                            case ExpressionType.LeftShiftAssign: return Expression.LeftShiftAssign(left, right);
                            case ExpressionType.RightShift: return Expression.RightShift(left, right);
                            case ExpressionType.RightShiftAssign: return Expression.RightShiftAssign(left, right);
                            case ExpressionType.NotEqual: return Expression.NotEqual(left, right);
                            case ExpressionType.Power: return Expression.Power(left, right);
                            case ExpressionType.PowerAssign: return Expression.PowerAssign(left, right);
                            case ExpressionType.Subtract: return Expression.Subtract(left, right);
                            case ExpressionType.SubtractChecked: return Expression.SubtractChecked(left, right);
                            case ExpressionType.SubtractAssign: return Expression.SubtractAssign(left, right);
                            case ExpressionType.SubtractAssignChecked: return Expression.SubtractAssignChecked(left, right);

                            default: throw new NotImplementedException();
                        }
                    }

                case ExpressionType.ArrayLength:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.OnesComplement:
                    {
                        var operand = deserialize(data, ref index, parameters);
                        switch (nodeType)
                        {
                            case ExpressionType.ArrayLength: return Expression.ArrayLength(operand);
                            case ExpressionType.Negate: return Expression.Negate(operand);
                            case ExpressionType.NegateChecked: return Expression.NegateChecked(operand);
                            case ExpressionType.Not: return Expression.Not(operand);
                            case ExpressionType.Quote: return Expression.Quote(operand);
                            case ExpressionType.PreIncrementAssign: return Expression.PreIncrementAssign(operand);
                            case ExpressionType.PreDecrementAssign: return Expression.PreDecrementAssign(operand);
                            case ExpressionType.PostIncrementAssign: return Expression.PostIncrementAssign(operand);
                            case ExpressionType.PostDecrementAssign: return Expression.PostDecrementAssign(operand);
                            case ExpressionType.IsTrue: return Expression.IsTrue(operand);
                            case ExpressionType.IsFalse: return Expression.IsFalse(operand);
                            case ExpressionType.OnesComplement: return Expression.OnesComplement(operand);
                            default: throw new NotImplementedException();
                        }
                    }

                case ExpressionType.Index:
                    {
                        var obj = deserialize(data, ref index, parameters);
                        var count = data[index++];
                        var args = new Expression[count];
                        for (var i = 0; i < count; i++)
                            args[i] = deserialize(data, ref index, parameters);

                        return Expression.ArrayAccess(obj, args);
                    }

                case ExpressionType.Unbox:
                case ExpressionType.Convert:
                case ExpressionType.TypeAs:
                case ExpressionType.ConvertChecked:
                    {
                        var value = deserialize(data, ref index, parameters);
                        var type = getType(data, ref index);

                        if (nodeType == ExpressionType.Convert)
                        {
                            if (type == typeof(Enum))
                            {
                                var interType = default(Type);

                                switch (Type.GetTypeCode(value.Type))
                                {
                                    case TypeCode.Byte: interType = typeof(UnknownByteEnum); break;
                                    case TypeCode.SByte: interType = typeof(UnknownSbyteEnum); break;
                                    case TypeCode.Int16: interType = typeof(UnknownShortEnum); break;
                                    case TypeCode.UInt16: interType = typeof(UnknownUshortEnum); break;
                                    case TypeCode.Int32: interType = typeof(UnknownIntEnum); break;
                                    case TypeCode.UInt32: interType = typeof(UnknownUintEnum); break;
                                    case TypeCode.Int64: interType = typeof(UnknownLongEnum); break;
                                    case TypeCode.UInt64: interType = typeof(UnknownUlongEnum); break;
                                }

                                value = Expression.Call(
                                    typeof(Enum).GetMethod("ToObject", new[] { typeof(Type), value.Type }),
                                    Expression.Constant(interType),
                                    value);
                            }

                            return Expression.Convert(value, type);
                        }

                        if (nodeType == ExpressionType.TypeAs)
                            return Expression.TypeAs(value, type);

                        if (nodeType == ExpressionType.Unbox)
                            return Expression.Unbox(value, type);

                        return Expression.ConvertChecked(value, type);
                    }

                case ExpressionType.MemberAccess:
                    {
                        var expr = deserialize(data, ref index, parameters);

                        var type = getType(data, ref index);

                        var memberName = getString(data, ref index);
                        var member = _MetadataWrappersCache.GetFieldOrProp(type, memberName);

                        return Expression.MakeMemberAccess(expr, member);
                    }

                case ExpressionType.Parameter:
                    {
                        var parameterIndex = getInt16(data, ref index);
                        var parameter = parameters[parameterIndex];
                        return parameter;
                    }

                case ExpressionType.Lambda:
                    {
                        var oldParamsCount = parameters.Count;
                        var lambdaParameters = deserializeVariables(data, ref index, parameters);

                        var body = deserialize(data, ref index, parameters);

                        parameters.RemoveRange(oldParamsCount, parameters.Count - oldParamsCount);

                        return Expression.Lambda(body, lambdaParameters);
                    }

                case ExpressionType.Constant:
                    {
                        var type = getType(data, ref index);
                        var value = getValue(data, ref index, type);

                        //if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        //    type = typeof(object);

                        return Expression.Constant(value, type);
                    }

                case ExpressionType.ArrayIndex:
                    {
                        var array = deserialize(data, ref index, parameters);
                        var arrIndex = deserialize(data, ref index, parameters);
                        return Expression.ArrayIndex(array, arrIndex);
                    }

                case ExpressionType.Conditional:
                    {
                        var type = getType(data, ref index);
                        var test = deserialize(data, ref index, parameters);
                        var ifTrue = deserialize(data, ref index, parameters);
                        var ifFalse = deserialize(data, ref index, parameters);

                        if (!type.IsAssignableFrom(ifTrue.Type))
                            ifTrue = Expression.Convert(ifTrue, type);

                        if (!type.IsAssignableFrom(ifFalse.Type))
                            ifFalse = Expression.Convert(ifFalse, type);

                        /*if (ifTrue.Type != ifFalse.Type)
                        {
                            if (ifFalse.Type.IsClass && ifTrue is ConstantExpression const0 && const0.Value == null)
                                ifTrue = Expression.Convert(ifTrue, ifFalse.Type);

                            if (ifTrue.Type.IsClass && ifFalse is ConstantExpression const1 && const1.Value == null)
                                ifFalse = Expression.Convert(ifFalse, ifTrue.Type);

                            var type = ifTrue.Type;
                            while (!type.IsAssignableFrom(ifFalse.Type))
                            {
                                type = type.BaseType;
                            }
                        }*/

                        return Expression.Condition(test, ifTrue, ifFalse, type);
                    }

                case ExpressionType.Call:
                    {
                        var target = deserialize(data, ref index, parameters);
                        var declType = getType(data, ref index);
                        var name = getString(data, ref index);
                        var count = getInt16(data, ref index);
                        var arguments = new Expression[count];
                        var types = new Type[count];
                        for (var i = 0; i < count; i++)
                        {
                            var argExp = deserialize(data, ref index, parameters);
                            arguments[i] = argExp;
                            types[i] = argExp.Type;
                        }

                        var isGeneric = data[index++] != 0;

                        MethodInfo method = null;
                        if (isGeneric)
                        {
                            var genericArgumentsCount = data[index++];
                            var genericArguments = new Type[genericArgumentsCount];
                            var i = 0;
                            for (; i < genericArgumentsCount; i++)
                            {
                                genericArguments[i] = getType(data, ref index);
                            }

                            var allMethods = _MetadataWrappersCache.GetMethods(declType);

                            i = 0;
                            for (; i < allMethods.Length; i++)
                            {
                                if (allMethods[i].Name == name)
                                    break;
                            }

                            for (; i < allMethods.Length; i++)
                            {
                                var methodInfo = allMethods[i];

                                if (methodInfo.Name != name)
                                    break;

                                if (methodInfo.IsGenericMethodDefinition
                                    && methodInfo.GetParameters().Length == types.Length
                                    && methodInfo.GetGenericArguments().Length == genericArguments.Length)
                                {
                                    var defMethod = methodInfo.MakeGenericMethod(genericArguments);

                                    var prms = defMethod.GetParameters();
                                    var suit = true;
                                    for (var j = 0; j < prms.Length; j++)
                                    {
                                        if (!prms[j].ParameterType.IsAssignableFrom(types[j]))
                                        {
                                            suit = false;
                                            break;
                                        }
                                    }

                                    if (suit)
                                    {
                                        method = defMethod;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            method = declType.GetMethod(name, types);
                        }

                        if (method == null)
                            throw new InvalidOperationException("Unable to deserialize method call");

                        if (method.IsStatic)
                            return Expression.Call(method, arguments);

                        return Expression.Call(target, method, arguments);
                    }

                case ExpressionType.Invoke:
                    {
                        var lambda = deserialize(data, ref index, parameters);
                        var count = getInt16(data, ref index);
                        var args = new Expression[count];
                        for (var i = 0; i < count; i++)
                            args[i] = deserialize(data, ref index, parameters);

                        return Expression.Invoke(lambda, args);
                    }

                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    {
                        var type = getType(data, ref index);
                        var count = getInt32(data, ref index);
                        var args = new Expression[count];
                        for (var i = 0; i < count; i++)
                            args[i] = deserialize(data, ref index, parameters);

                        if (nodeType == ExpressionType.NewArrayInit)
                            return Expression.NewArrayInit(type, args);

                        return Expression.NewArrayBounds(type, args);
                    }

                case ExpressionType.Block:
                    {
                        var expressionsCount = getInt32(data, ref index);

                        var oldParamsCount = parameters.Count;
                        var variables = deserializeVariables(data, ref index, parameters);

                        var body = new Expression[expressionsCount];
                        for (var i = 0; i < expressionsCount; i++)
                        {
                            body[i] = deserialize(data, ref index, parameters);
                        }

                        parameters.RemoveRange(oldParamsCount, parameters.Count - oldParamsCount);

                        return Expression.Block(variables, body);
                    }

                case ExpressionType.New:
                    {
                        var type = getType(data, ref index);
                        var argsCount = getInt16(data, ref index);
                        var args = new Expression[argsCount];
                        var types = new Type[argsCount];
                        for (var i = 0; i < argsCount; i++)
                        {
                            args[i] = deserialize(data, ref index, parameters);
                            types[i] = args[i].Type;
                        }

                        return Expression.New(type.GetConstructor(types), args);
                    }

                default: throw new NotImplementedException(nodeType.ToString());
            }
        }

        private ParameterExpression[] deserializeVariables(byte[] data, ref int index, List<ParameterExpression> parameters)
        {
            var paramsCount = getInt16(data, ref index);
            var lambdaParameters = new ParameterExpression[paramsCount];
            for (var i = 0; i < paramsCount; i++)
            {
                var type = getType(data, ref index);
                var name = getString(data, ref index);
                var parameter = Expression.Parameter(type, name);
                lambdaParameters[i] = parameter;
                parameters.Add(parameter);
            }

            return lambdaParameters;
        }

        private object getValue(byte[] data, ref int index, Type type)
        {
            if (type != null && type.IsArray)
            {
                var len = getInt32(data, ref index);
                if (len == -1)
                    return null;

                var array = _MetadataWrappersCache.CreateArray(type, len);

                if (array is byte[] byteArray)
                {
                    for (var i = 0; i < len; i++)
                        byteArray[i] = data[index++];
                }
                else if (array is int[] intArray)
                {
                    for (var i = 0; i < len; i++)
                        intArray[i] = getInt32(data, ref index);
                }
                else if (array is uint[] uintArray)
                {
                    for (var i = 0; i < len; i++)
                        uintArray[i] = (uint)getInt32(data, ref index);
                }
                else
                {
                    var elementType = type.GetElementType();

                    for (var i = 0; i < len; i++)
                        array.SetValue(getValue(data, ref index, elementType), i);
                }

                return array;
            }

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Byte: return data[index++];
                case TypeCode.SByte: return (sbyte)data[index++];

                case TypeCode.Char: return (char)getInt16(data, ref index);

                case TypeCode.Int16: return getInt16(data, ref index);
                case TypeCode.UInt16: return (ushort)getInt16(data, ref index);

                case TypeCode.Int32: return getInt32(data, ref index);
                case TypeCode.UInt32: return (uint)getInt32(data, ref index);

                case TypeCode.Int64: return getInt64(data, ref index);
                case TypeCode.UInt64: return (ulong)getInt64(data, ref index);

                case TypeCode.Single:
                    unsafe
                    {
                        var value = getInt32(data, ref index);
                        return *(float*)&value;
                    }

                case TypeCode.Double:
                    unsafe
                    {
                        var value = getInt64(data, ref index);
                        return (*(double*)&value);
                    }

                case TypeCode.Decimal:
                    unsafe
                    {
                        var d = decimal.Zero;
                        var p = (byte*)&d;
                        for (var i = 0; i < sizeof(decimal); i++)
                            *p++ = data[index++];
                        return (d);
                    }

                case TypeCode.Boolean: return data[index++] != 0;

                case TypeCode.String: return getString(data, ref index);

                case TypeCode.DateTime: return DateTime.FromBinary(getInt64(data, ref index));

                case TypeCode.Object:
                    {
                        if (type == typeof(object))
                        {
                            var nestedType = getType(data, ref index);
                            return getValue(data, ref index, nestedType);
                        }

                        if (type.IsValueType)
                        {
                            var members = _MetadataWrappersCache.GetFieldAndProps(type);
                            var result = Activator.CreateInstance(type);
                            for (var i = 0; i < members.Length; i++)
                            {
                                if (members[i] is FieldInfo field)
                                {
                                    field.SetValue(result, getValue(data, ref index, field.FieldType));
                                }
                            }

                            return result;
                        }

                        if (typeof(Type).IsAssignableFrom(type))
                        {
                            return Type.GetType(getString(data, ref index));
                        }

                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                            return getValue(data, ref index, type.GetGenericArguments()[0]);

                        if (data[index] == 0)
                        {
                            index++;
                            return null;
                        }

                        throw new NotSupportedException(type.ToString());
                    }

                case TypeCode.Empty: return null;

                case TypeCode.DBNull: throw new NotSupportedException(typeCode.ToString());

                default: throw new NotImplementedException();
            }
        }

        private static short getInt16(byte[] data, ref int index)
        {
            return (short)(data[index++] | (data[index++] << 8));
        }

        private static int getInt32(byte[] data, ref int index)
        {
            var lo = (int)(ushort)getInt16(data, ref index);
            var hi = (ushort)getInt16(data, ref index);
            return lo | (hi << 16);
        }

        private static long getInt64(byte[] data, ref int index)
        {
            var lo = (long)(uint)getInt32(data, ref index);
            var hi = (long)(uint)getInt32(data, ref index);
            return lo | (hi << 32);
        }

        private Type getType(byte[] data, ref int index)
        {
            var typeCode = (TypeCode)data[index++];
            switch (typeCode)
            {
                case TypeCode.Byte: return typeof(byte);
                case TypeCode.SByte: return typeof(sbyte);
                case TypeCode.Int16: return typeof(short);
                case TypeCode.UInt16: return typeof(ushort);
                case TypeCode.Int32: return typeof(int);
                case TypeCode.UInt32: return typeof(uint);
                case TypeCode.Int64: return typeof(long);
                case TypeCode.UInt64: return typeof(ulong);
                case TypeCode.Single: return typeof(float);
                case TypeCode.Double: return typeof(double);
                case TypeCode.Decimal: return typeof(decimal);
                case TypeCode.Boolean: return typeof(bool);
                case TypeCode.Char: return typeof(char);
                case TypeCode.String: return typeof(string);
                case TypeCode.DateTime: return typeof(DateTime);
                case TypeCode.Object: return typeof(object);

                case AdditionalTypeCodes._ArrayTypeCode: return getType(data, ref index).MakeArrayType();
                case AdditionalTypeCodes._BoxiedTypeCode: return typeof(Nullable<>).MakeGenericType(getType(data, ref index));
                case AdditionalTypeCodes._RegisteredTypeCode:
                    {
                        var exTypeCode = (uint)getInt32(data, ref index);
                        return _types.GetType(exTypeCode);
                    }
                case AdditionalTypeCodes._UnregisteredTypeCode:
                    {
                        var typeName = getString(data, ref index);
                        lock (_loadedType)
                        {
                            if (!_loadedType.TryGetValue(typeName, out var type))
                            {
                                type = Type.GetType(typeName);

                                if (type == null)
                                    type = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(typeName)).FirstOrDefault(x => x != null);

                                if (type != null)
                                    _loadedType[typeName] = type;
                                else
                                    throw new KeyNotFoundException("Unable to resolve type \"" + typeName + "\"");
                            }

                            return type;
                        }
                    }

                case TypeCode.Empty: return null;

                case TypeCode.DBNull: throw new NotSupportedException(typeCode.ToString());

                default: throw new NotImplementedException();
            }
        }

        private unsafe static string getString(byte[] data, ref int index)
        {
            var len = getInt16(data, ref index);
            if (len == -1)
                return null;

            var buffer = stackalloc char[len];
            for (var i = 0; i < len; i++)
                buffer[i] = (char)getInt16(data, ref index);

            return new string(buffer, 0, len);
        }
    }
}
