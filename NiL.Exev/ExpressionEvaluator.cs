using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NiL.Exev
{
    public sealed class Parameter
    {
        public readonly ParameterExpression ParameterExpression;
        public object Value;

        public Parameter(ParameterExpression parameterExpression)
        {
            ParameterExpression = parameterExpression ?? throw new ArgumentNullException(nameof(parameterExpression));
        }

        public Parameter(ParameterExpression parameterExpression, object value)
            : this(parameterExpression)
        {
            if (!parameterExpression.Type.IsAssignableFrom(value.GetType()))
                throw new ArgumentException("Invalid value type");

            Value = value;
        }
    }

    public sealed partial class ExpressionEvaluator
    {
        public object Eval(Expression expression, params Parameter[] parameters)
        {
            if (expression == null)
                throw new ArgumentNullException();

            var stack = new Stack<object>();
            eval(expression, stack, parameters);

            return stack.Pop();
        }

        private void eval(Expression expression, Stack<object> stack, Parameter[] parameters)
        {
            if (expression is ConstantExpression constantExpression)
            {
                stack.Push(constantExpression.Value);
            }
            else if (expression.NodeType == ExpressionType.Index
                || expression.NodeType == ExpressionType.ArrayIndex
                || expression.NodeType == ExpressionType.Parameter)
            {
                evalAccess(expression, stack, parameters, true, out _, out _);
            }
            else if (expression is MemberExpression memberExpression)
            {
                eval(memberExpression.Expression, stack, parameters);
                stack.Push(MetadataWrappersCache.GetMemberValue(stack, memberExpression.Member));
            }
            else if (expression.NodeType == ExpressionType.Assign)
            {
                var binaryExpression = expression as BinaryExpression;

                evalAccess(
                    binaryExpression.Left,
                    stack,
                    parameters,
                    false,
                    out object targetObject,
                    out object targetProperty);

                eval(binaryExpression.Right, stack, parameters);

                assign(stack, parameters, binaryExpression.Left.NodeType, targetObject, targetProperty);
            }
            else if (expression is BinaryExpression binaryExpression)
            {
                var targetObject = default(object);
                var targetProperty = default(object);
                var assign = expression.NodeType >= ExpressionType.AddAssign && expression.NodeType <= ExpressionType.PostDecrementAssign;
                if (assign)
                    evalAccess(binaryExpression.Left, stack, parameters, true, out targetObject, out targetProperty);
                else
                    eval(binaryExpression.Left, stack, parameters);
                eval(binaryExpression.Right, stack, parameters);

                var right = stack.Pop();
                var left = stack.Pop();

                var type = binaryExpression.Left.Type;

                switch (expression.NodeType)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddAssign:
                        stack.Push(add_unchecked(left, right, type));
                        break;

                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractAssign:
                        stack.Push(sub_unchecked(left, right, type));
                        break;

                    case ExpressionType.Divide:
                    case ExpressionType.DivideAssign:
                        stack.Push(div_unchecked(left, right, type));
                        break;

                    case ExpressionType.Modulo:
                    case ExpressionType.ModuloAssign:
                        stack.Push(mod_unchecked(left, right, type));
                        break;

                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyAssign:
                        stack.Push(mul_unchecked(left, right, type));
                        break;

                    case ExpressionType.And:
                    case ExpressionType.AndAssign:
                        stack.Push(and_unchecked(left, right, type));
                        break;

                    case ExpressionType.ExclusiveOr:
                    case ExpressionType.ExclusiveOrAssign:
                        stack.Push(xor_unchecked(left, right, type));
                        break;

                    case ExpressionType.Or:
                    case ExpressionType.OrAssign:
                        stack.Push(or_unchecked(left, right, type));
                        break;

                    case ExpressionType.AndAlso:
                        stack.Push((bool)left && (bool)right);
                        break;

                    case ExpressionType.AddChecked:
                    case ExpressionType.AddAssignChecked:
                        stack.Push(add_checked(left, right, type));
                        break;

                    case ExpressionType.SubtractChecked:
                    case ExpressionType.SubtractAssignChecked:
                        stack.Push(sub_checked(left, right, type));
                        break;

                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.MultiplyAssignChecked:
                        stack.Push(mul_checked(left, right, type));
                        break;

                    case ExpressionType.GreaterThan:
                        stack.Push(more_unchecked(left, right, type));
                        break;

                    case ExpressionType.GreaterThanOrEqual:
                        stack.Push(moreOrEqual_unchecked(left, right, type));
                        break;

                    case ExpressionType.LessThan:
                        stack.Push(less_unchecked(left, right, type));
                        break;

                    case ExpressionType.LessThanOrEqual:
                        stack.Push(lessOrEqual_unchecked(left, right, type));
                        break;

                    case ExpressionType.Equal:
                        stack.Push(equal_unchecked(left, right, type));
                        break;

                    case ExpressionType.NotEqual:
                        stack.Push(notEqual_unchecked(left, right, type));
                        break;

                    case ExpressionType.RightShift:
                    case ExpressionType.RightShiftAssign:
                        stack.Push(rightShift_unchecked(left, right, type));
                        break;

                    case ExpressionType.LeftShift:
                    case ExpressionType.LeftShiftAssign:
                        stack.Push(leftShift_unchecked(left, right, type));
                        break;

                    default: throw new NotImplementedException(expression.NodeType.ToString());
                }

                if (assign)
                    ExpressionEvaluator.assign(stack, parameters, binaryExpression.Left.NodeType, targetObject, targetProperty);
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                eval(unaryExpression.Operand, stack, parameters);

                switch (expression.NodeType)
                {
                    case ExpressionType.ArrayLength:
                        stack.Push(((Array)stack.Pop()).Length);
                        break;

                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        stack.Push(Convert.ChangeType(stack.Pop(), unaryExpression.Type));
                        break;

                    default: throw new NotImplementedException(expression.NodeType.ToString());
                }
            }
            else if (expression is NewArrayExpression newArrayExpression)
            {
                var type = newArrayExpression.Type;

                if (newArrayExpression.Expressions.Count == 1)
                {
                    eval(newArrayExpression.Expressions[0], stack, parameters);
                    var length = (int)stack.Pop();

                    stack.Push(MetadataWrappersCache.CreateArray(type, length));
                }
                else
                {
                    var lengths = new int[newArrayExpression.Expressions.Count];
                    for (var i = 0; i < lengths.Length; i++)
                    {
                        eval(newArrayExpression.Expressions[i], stack, parameters);
                        lengths[i] = (int)stack.Pop();
                    }

                    var array = Array.CreateInstance(type.GetElementType(), lengths);
                    stack.Push(array);
                }
            }
            else if (expression is MethodCallExpression callExpression)
            {
                var method = callExpression.Method;
                var lambda = MetadataWrappersCache.GetMethod(method);

                if (!method.IsStatic)
                    eval(callExpression.Object, stack, parameters);

                for (var i = 0; i < callExpression.Arguments.Count; i++)
                    eval(callExpression.Arguments[i], stack, parameters);

                stack.Push(lambda(stack));
            }
            else if (expression is InvocationExpression invocationExpression)
            {
                eval(invocationExpression.Expression, stack, parameters);
                var srcLambda = (Delegate)stack.Peek();
                var lambda = MetadataWrappersCache.WrapLambda(srcLambda);

                for (var i = 0; i < invocationExpression.Arguments.Count; i++)
                    eval(invocationExpression.Arguments[i], stack, parameters);

                stack.Push(lambda(stack));
            }
            else if (expression is NewExpression newExpression)
            {
                for (var i = 0; i < newExpression.Arguments.Count; i++)
                    eval(newExpression.Arguments[i], stack, parameters);

                stack.Push(MetadataWrappersCache.GetCtor(newExpression.Constructor)(stack));
            }
            else throw new NotImplementedException(expression.NodeType.ToString());
        }

        private static void assign(Stack<object> stack, Parameter[] parameters, ExpressionType targetExpressionType, object targetObject, object targetProperty)
        {
            switch (targetExpressionType)
            {
                case ExpressionType.Parameter:
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterExpression == targetObject)
                        {
                            parameters[i].Value = stack.Peek();
                            break;
                        }
                    }

                    break;
                }

                case ExpressionType.Index:
                {
                    ((Array)targetObject).SetValue(stack.Peek(), (long[])targetProperty);
                    break;
                }

                default: throw new NotImplementedException("assign for " + targetExpressionType);
            }
        }

        private void evalAccess(Expression expression, Stack<object> stack, Parameter[] parameters, bool computeValue, out object targetObject, out object member)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                {
                    targetObject = expression;
                    member = null;

                    if (!computeValue)
                        return;

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterExpression == expression)
                        {
                            stack.Push(parameters[i].Value);
                            return;
                        }
                    }

                    break;
                }

                case ExpressionType.Index when expression is IndexExpression indexExpression:
                {
                    eval(indexExpression.Object, stack, parameters);
                    var array = stack.Pop();

                    var indexes = new long[indexExpression.Arguments.Count];
                    for (var i = 0; i < indexes.Length; i++)
                    {
                        eval(indexExpression.Arguments[i], stack, parameters);
                        var index = stack.Pop();
                        if (index is int iindex)
                            indexes[i] = iindex;
                        else if (index is long lindex)
                            indexes[i] = lindex;
                        else
                            throw new InvalidOperationException("Unknown index type (" + (index?.GetType().FullName ?? "<null>") + ")");
                    }

                    targetObject = array;
                    member = indexes;

                    if (!computeValue)
                        return;

                    stack.Push(((Array)array).GetValue(indexes));
                    break;
                }

                default: throw new NotImplementedException("lvalue for " + expression.NodeType);
            }
        }
    }
}
