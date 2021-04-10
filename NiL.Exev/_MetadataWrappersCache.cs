using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NiL.Exev
{
    internal static class _MetadataWrappersCache
    {
        private static readonly MethodInfo _StackPopMethod = typeof(Stack<object>).GetMethod(nameof(Stack<object>.Pop));
        private static readonly ParameterExpression _StackPrm = Expression.Parameter(typeof(Stack<object>), "stack");

        private static readonly Dictionary<Type, Func<int, Array>> _ArraysCtors = new Dictionary<Type, Func<int, Array>>();
        private static readonly Dictionary<MethodInfo, Func<Stack<object>, object>> _ProxiedMethods = new Dictionary<MethodInfo, Func<Stack<object>, object>>();
        private static readonly Dictionary<ConstructorInfo, Func<Stack<object>, object>> _ProxiedCtors = new Dictionary<ConstructorInfo, Func<Stack<object>, object>>();
        private static readonly Dictionary<MemberInfo, Func<Stack<object>, object>> _ProxiedMembersSetters = new Dictionary<MemberInfo, Func<Stack<object>, object>>();
        private static readonly Dictionary<Type, Func<Stack<object>, object>> _ProxiedDelegates = new Dictionary<Type, Func<Stack<object>, object>>();
        private static readonly Dictionary<Type, MethodInfo[]> _TypeMethods = new Dictionary<Type, MethodInfo[]>();
        private static readonly Dictionary<(Type, string), MemberInfo> _TypeMembers = new Dictionary<(Type, string), MemberInfo>();

        internal static MethodInfo[] GetMethods(Type type)
        {
            if (_TypeMethods.TryGetValue(type, out var result))
                return result;

            _TypeMethods[type] = result = type.GetMethods();
            Array.Sort(result, (x, y) => string.CompareOrdinal(x.Name, y.Name));

            return result;
        }

        internal static Array CreateArray(Type type, int length)
        {
            lock (_ArraysCtors)
            {
                if (_ArraysCtors.TryGetValue(type, out var ctor))
                    return ctor(length);

                var param = Expression.Parameter(typeof(int), "len");
                var expr = Expression.New(type.GetConstructor(new[] { typeof(int) }), param);
                var lambda = Expression.Lambda<Func<int, Array>>(expr, param).Compile();

                _ArraysCtors.Add(type, lambda);

                return lambda(length);
            }
        }

        internal static Func<Stack<object>, object> GetCtor(ConstructorInfo constructor)
        {
            lock (_ProxiedCtors)
            {
                if (_ProxiedCtors.TryGetValue(constructor, out var ctor))
                    return ctor;

                var parameters = constructor.GetParameters();
                var body = new Expression[parameters.Length + 1];
                var variables = new ParameterExpression[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    var variable = Expression.Parameter(parameters[i].ParameterType, parameters[i].Name);
                    variables[i] = variable;
                    body[parameters.Length - i - 1] = Expression.Assign(variable, Expression.Convert(Expression.Call(_StackPrm, _StackPopMethod), parameters[i].ParameterType));
                }

                body[parameters.Length] = Expression.New(constructor, variables);

                var methodBody = (Expression)Expression.Block(variables, body);

                if (methodBody.Type.IsValueType)
                {
                    methodBody = Expression.Convert(methodBody, typeof(object));
                }

                ctor = Expression.Lambda<Func<Stack<object>, object>>(methodBody, _StackPrm).Compile();

                _ProxiedCtors[constructor] = ctor;
                return ctor;
            }
        }

        internal static Func<Stack<object>, object> GetMethod(MethodInfo methodInfo)
        {
            lock (_ProxiedMethods)
            {
                if (_ProxiedMethods.TryGetValue(methodInfo, out var method))
                    return method;

                method = proxyMethod(methodInfo);

                _ProxiedMethods[methodInfo] = method;
                return method;
            }
        }

        private static Func<Stack<object>, object> proxyMethod(MethodInfo methodInfo)
        {
            Func<Stack<object>, object> method;
            var parameters = methodInfo.GetParameters();
            var body = new Expression[parameters.Length + 1];
            var variables = new ParameterExpression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var variable = Expression.Parameter(parameters[i].ParameterType, parameters[i].Name);
                variables[i] = variable;
                body[parameters.Length - i - 1] = Expression.Assign(variable, Expression.Convert(Expression.Call(_StackPrm, _StackPopMethod), parameters[i].ParameterType));
            }

            if (methodInfo.IsStatic)
                body[parameters.Length] = Expression.Call(methodInfo, variables);
            else
                body[parameters.Length] = Expression.Call(Expression.Convert(Expression.Call(_StackPrm, _StackPopMethod), methodInfo.DeclaringType), methodInfo, variables);

            var methodBody = (Expression)Expression.Block(variables, body);

            if (methodBody.Type.IsValueType)
            {
                if (methodBody.Type == typeof(void))
                {
                    methodBody = Expression.Block(methodBody, Expression.Constant(null));
                }
                else
                {
                    methodBody = Expression.Convert(methodBody, typeof(object));
                }
            }

            method = Expression.Lambda<Func<Stack<object>, object>>(methodBody, _StackPrm).Compile();
            return method;
        }

        internal static Func<Stack<object>, object> WrapLambda(Delegate srcLambda)
        {
            lock (_ProxiedDelegates)
            {
                var type = srcLambda.GetType();
                if (_ProxiedDelegates.TryGetValue(type, out var method))
                    return method;

                method = proxyMethod(type.GetMethod("Invoke"));

                _ProxiedDelegates.Add(type, method);

                return method;
            }
        }

        internal static object GetMemberValue(Stack<object> stack, MemberInfo member)
        {
            var parameter = Expression.Variable(typeof(object), "x");

            var memberAccess = (Expression)Expression.MakeMemberAccess(Expression.Convert(parameter, member.DeclaringType), member);
            if (memberAccess.Type.IsValueType)
                memberAccess = Expression.Convert(memberAccess, typeof(object));

            var expr = Expression.Lambda<Func<object, object>>(memberAccess, parameter);
            expr.Reduce();
            var lambda = expr.Compile();

            _ProxiedMembersSetters[member] = lambda;

            return lambda(stack.Pop());
        }

        internal static MemberInfo GetMember(Type type, string memberName)
        {
            if (_TypeMembers.TryGetValue((type, memberName), out var memberInfo))
                return memberInfo;

            memberInfo = type.GetMember(memberName).SingleOrDefault();
            if (memberInfo != null)
                _TypeMembers[(type, memberName)] = memberInfo;

            return memberInfo;
        }
    }
}
