using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NiL.Exev
{
    public sealed partial class ExpressionEvaluator
    {
        private readonly Action<MemberInfo> _memberAccessValidator;

        public ExpressionEvaluator()
        {
        }

        public ExpressionEvaluator(Func<MemberInfo, bool> memberAccessValidator)
        {
            if (memberAccessValidator is null)
                throw new ArgumentNullException(nameof(memberAccessValidator));

            _memberAccessValidator = x =>
            {
                if (!memberAccessValidator(x))
                    throw new MemberAccessException(x.ToString());
            };
        }

        public object Eval(Expression expression, params Parameter[] parameters)
        {
            if (expression == null)
                throw new ArgumentNullException();

            var stack = new Stack<object>();
            eval(expression, stack, new List<Parameter>(parameters));

            return stack.Pop();
        }

        private void eval(Expression expression, Stack<object> stack, List<Parameter> parameters)
        {
            if (expression is ConstantExpression constantExpression)
            {
                stack.Push(constantExpression.Value);
            }
            else if (expression.NodeType == ExpressionType.Index
                || expression.NodeType == ExpressionType.ArrayIndex
                || expression.NodeType == ExpressionType.Parameter
                || expression.NodeType == ExpressionType.MemberAccess)
            {
                evalAccess(expression, stack, parameters);
            }
            else if (expression is MemberExpression memberExpression)
            {
                eval(memberExpression.Expression, stack, parameters);
                stack.Push(_MetadataWrappersCache.GetMemberValue(stack, memberExpression.Member));
            }
            else if (expression.NodeType == ExpressionType.Assign)
            {
                var binaryExpression = expression as BinaryExpression;

                assign(stack, parameters, binaryExpression.Left, binaryExpression.Right);
            }
            else if (expression is BinaryExpression binaryExpression)
            {
                var assign = expression.NodeType >= ExpressionType.AddAssign && expression.NodeType <= ExpressionType.PostDecrementAssign;
                if (assign)
                    evalAccess(binaryExpression.Left, stack, parameters);
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
                        if (!type.IsPrimitive)
                            stack.Push(Equals(left, right));
                        else
                            stack.Push(equal_unchecked(left, right, type));
                        break;

                    case ExpressionType.NotEqual:
                        if (!type.IsPrimitive)
                            stack.Push(!Equals(left, right));
                        else
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
                    this.assign(stack, parameters, binaryExpression.Left, null);
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

                    case ExpressionType.TypeAs:
                        break;

                    case ExpressionType.Not:
                        stack.Push(!(bool)stack.Pop());
                        break;

                    default: throw new NotImplementedException(expression.NodeType.ToString());
                }
            }
            else if (expression is NewArrayExpression newArrayExpression)
            {
                var type = newArrayExpression.Type;

                int itemsCount = newArrayExpression.Expressions.Count;

                if (newArrayExpression.NodeType == ExpressionType.NewArrayBounds)
                {
                    var lengths = new int[itemsCount];

                    for (var i = 0; i < itemsCount; i++)
                    {
                        eval(newArrayExpression.Expressions[i], stack, parameters);
                        lengths[i] = (int)stack.Pop();
                    }

                    var array = Array.CreateInstance(type.GetElementType(), lengths);

                    stack.Push(array);
                }
                else
                {
                    var array = (Array)Activator.CreateInstance(type, itemsCount);

                    for (var i = 0; i < itemsCount; i++)
                    {
                        eval(newArrayExpression.Expressions[i], stack, parameters);
                        array.SetValue(stack.Pop(), i);
                    }

                    stack.Push(array);
                }
            }
            else if (expression is MethodCallExpression callExpression)
            {
                var method = callExpression.Method;

                _memberAccessValidator?.Invoke(method);

                var lambda = _MetadataWrappersCache.GetMethod(method);

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
                var lambda = _MetadataWrappersCache.WrapLambda(srcLambda);

                for (var i = 0; i < invocationExpression.Arguments.Count; i++)
                    eval(invocationExpression.Arguments[i], stack, parameters);

                stack.Push(lambda(stack));
            }
            else if (expression is NewExpression newExpression)
            {
                for (var i = 0; i < newExpression.Arguments.Count; i++)
                    eval(newExpression.Arguments[i], stack, parameters);

                _memberAccessValidator?.Invoke(newExpression.Constructor);

                stack.Push(_MetadataWrappersCache.GetCtor(newExpression.Constructor)(stack));
            }
            else if (expression is MemberInitExpression memberInit)
            {
                eval(memberInit.NewExpression, stack, parameters);

                for (var i = 0; i < memberInit.Bindings.Count; i++)
                {
                    var targetObject = stack.Peek();
                    switch (memberInit.Bindings[i])
                    {
                        case MemberAssignment assignment:
                        {
                            eval(assignment.Expression, stack, parameters);
                            var member = assignment.Member;
                            assignMember(stack, stack.Pop(), member);

                            stack.Push(targetObject);
                            break;
                        }

                        case MemberListBinding memberListBinding:
                        {
                            MethodInfo prevAddMethod = null;
                            Func<Stack<object>, object> addMethod = null;
                            Type[] types = new Type[1];

                            for (var j = 0; j < memberListBinding.Initializers.Count; j++)
                            {
                                var item = memberListBinding.Initializers[j];

                                if (prevAddMethod != item.AddMethod)
                                {
                                    addMethod = _MetadataWrappersCache.GetMethod(item.AddMethod);
                                    prevAddMethod = item.AddMethod;
                                }

                                for (var k = 0; k < item.Arguments.Count; k++)
                                {
                                    eval(item.Arguments[k], stack, parameters);
                                }

                                addMethod(stack);
                                stack.Push(targetObject);
                            }

                            break;
                        }

                        default: throw new NotImplementedException(memberInit.Bindings[i].GetType().FullName);
                    }
                }
            }
            else if (expression is BlockExpression blockExpression)
            {
                var parametersCount = parameters.Count;
                for (var i = 0; i < blockExpression.Variables.Count; i++)
                    parameters.Add(new Parameter(blockExpression.Variables[i]));

                for (var i = 0; i < blockExpression.Expressions.Count; i++)
                {
                    stack.Clear();
                    eval(blockExpression.Expressions[i], stack, parameters);
                }

                parameters.RemoveRange(parametersCount, parameters.Count - parametersCount);
            }
            else if (expression is ConditionalExpression conditional)
            {
                eval(conditional.Test, stack, parameters);
                if ((bool)stack.Pop())
                {
                    eval(conditional.IfTrue, stack, parameters);
                }
                else
                {
                    eval(conditional.IfFalse, stack, parameters);
                }
            }
            else if (expression is LambdaExpression lambdaExpression)
            {
                var lambda = _DelegateWrapperCache.GetLambda(this, parameters, lambdaExpression);
                stack.Push(lambda);
            }
            else throw new NotImplementedException(expression.NodeType.ToString());
        }

        private void assign(Stack<object> stack, List<Parameter> parameters, Expression targetExpression, Expression sourceExpression)
        {
            switch (targetExpression.NodeType)
            {
                case ExpressionType.Parameter:
                {
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        if (parameters[i].ParameterExpression == targetExpression)
                        {
                            parameters[i].Value = stack.Peek();
                            break;
                        }
                    }

                    break;
                }

                case ExpressionType.Index when targetExpression is IndexExpression indexExpression:
                {
                    computeArrayTarget(stack, parameters, indexExpression, out object array, out long[] indexes);

                    if (sourceExpression != null)
                        eval(sourceExpression, stack, parameters);

                    ((Array)array).SetValue(stack.Peek(), indexes);
                    break;
                }

                case ExpressionType.MemberAccess when targetExpression is MemberExpression memberExpression:
                {
                    _memberAccessValidator?.Invoke(memberExpression.Member);

                    eval(memberExpression.Expression, stack, parameters);

                    if (sourceExpression != null)
                        eval(sourceExpression, stack, parameters);

                    var value = stack.Pop();
                    var member = memberExpression.Member;

                    assignMember(stack, value, member);

                    stack.Push(value);
                    break;
                }

                default: throw new NotImplementedException("assign for " + targetExpression.NodeType);
            }
        }

        private static void assignMember(Stack<object> stack, object value, MemberInfo member)
        {
            switch (member)
            {
                case PropertyInfo property:
                {
                    var setter = _MetadataWrappersCache.GetMethod(property.SetMethod);
                    stack.Push(value);
                    setter(stack);
                    break;
                }

                case FieldInfo field:
                {
                    var obj = stack.Pop();
                    field.SetValue(obj, value);
                    break;
                }

                default: throw new NotImplementedException("MemberAccess for " + member.GetType().Name);
            }
        }

        private void evalAccess(Expression expression, Stack<object> stack, List<Parameter> parameters)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                {
                    for (var i = parameters.Count; i-- > 0;)
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
                    computeArrayTarget(stack, parameters, indexExpression, out object array, out long[] indexes);

                    stack.Push(((Array)array).GetValue(indexes));
                    break;
                }

                case ExpressionType.MemberAccess when expression is MemberExpression memberExpression:
                {
                    _memberAccessValidator?.Invoke(memberExpression.Member);

                    var staticMember = memberExpression.Expression == null;

                    if (!staticMember)
                        eval(memberExpression.Expression, stack, parameters);

                    switch (memberExpression.Member)
                    {
                        case PropertyInfo property:
                        {
                            var getter = _MetadataWrappersCache.GetMethod(property.GetMethod);
                            var value = getter(stack);
                            stack.Push(value);
                            return;
                        }

                        case FieldInfo field:
                        {
                            var obj = staticMember ? null : stack.Pop();
                            var value = field.GetValue(obj);
                            stack.Push(value);
                            return;
                        }

                        default: throw new NotImplementedException("MemberAccess for " + memberExpression.Member.GetType().Name);
                    }
                }

                case ExpressionType.ArrayIndex when expression is BinaryExpression binaryExpression:
                {
                    eval(binaryExpression.Left, stack, parameters);
                    var array = (Array)stack.Pop();

                    eval(binaryExpression.Right, stack, parameters);
                    var index = (int)stack.Pop();

                    stack.Push(array.GetValue(index));
                    break;
                }

                default: throw new NotImplementedException("rvalue for " + expression.NodeType);
            }
        }

        private void computeArrayTarget(Stack<object> stack, List<Parameter> parameters, IndexExpression indexExpression, out object array, out long[] indexes)
        {
            eval(indexExpression.Object, stack, parameters);
            array = stack.Pop();
            indexes = new long[indexExpression.Arguments.Count];
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
        }
    }
}
