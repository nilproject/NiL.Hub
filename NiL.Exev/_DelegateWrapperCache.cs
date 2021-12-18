using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace NiL.Exev
{
    internal static class _DelegateWrapperCache
    {
        private readonly static MethodInfo _ResizeMethod = typeof(Array).GetMethod(nameof(Array.Resize)).MakeGenericMethod(typeof(Parameter));
        private readonly static MethodInfo _EvalMethod = typeof(ExpressionEvaluator).GetMethod(nameof(ExpressionEvaluator.Eval));
        private readonly static MethodInfo _CopyToMethod = typeof(List<Parameter>).GetMethod(nameof(List<Parameter>.CopyTo), new[] { typeof(Parameter[]) });
        private readonly static MethodInfo _IndexGetter = typeof(ReadOnlyCollection<ParameterExpression>).GetMethod("get_Item");
        private readonly static ConstructorInfo _ParameterCtor = typeof(Parameter).GetConstructor(new[] { typeof(ParameterExpression), typeof(object) });

        private readonly static Dictionary<Type, Func<ExpressionEvaluator, List<Parameter>, LambdaExpression, Delegate>> _cache
            = new Dictionary<Type, Func<ExpressionEvaluator, List<Parameter>, LambdaExpression, Delegate>>();

        internal static Delegate GetLambda(ExpressionEvaluator evaluator, List<Parameter> parameters, LambdaExpression expression)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(expression.Type, out var wrapper))
                    return wrapper(evaluator, parameters, expression);
            }

            var evalPrm = Expression.Parameter(typeof(ExpressionEvaluator), nameof(evaluator));
            var prmsPrm = Expression.Parameter(typeof(List<Parameter>), nameof(parameters));
            var exprPrm = Expression.Parameter(typeof(LambdaExpression), nameof(expression));

            var initialPrmsCountVar = Expression.Variable(typeof(int), "initialPrmsCount");
            var innerPrmsPrm = Expression.Variable(typeof(Parameter[]), "innerPrms");
            var outerBody = new List<Expression>();

            var prms = expression.Parameters;
            var prmsCount = expression.Parameters.Count;
            var exprPrmsProp = Expression.PropertyOrField(exprPrm, nameof(expression.Parameters));

            outerBody.Add(Expression.Assign(initialPrmsCountVar, Expression.PropertyOrField(prmsPrm, nameof(parameters.Count))));
            outerBody.Add(Expression.Assign(innerPrmsPrm, Expression.NewArrayBounds(typeof(Parameter), Expression.Add(initialPrmsCountVar, Expression.Constant(prmsCount)))));
            outerBody.Add(Expression.Call(prmsPrm, _CopyToMethod, innerPrmsPrm));
            //outerBody.Add(Expression.Call(_ResizeMethod, prmsPrm, Expression.Add(Expression.ArrayLength(prmsPrm), Expression.Constant(expression.Parameters.Count))));

            var innerBody = new List<Expression>();
            for (var i = 0; i < expression.Parameters.Count; i++)
            {
                innerBody.Add(
                    Expression.Assign(Expression.ArrayAccess(innerPrmsPrm, Expression.Add(initialPrmsCountVar, Expression.Constant(i))),
                    Expression.New(_ParameterCtor, Expression.Call(exprPrmsProp, _IndexGetter, Expression.Constant(i)), Expression.Convert(prms[i], typeof(object)))));
            }

            var evalCall = Expression.Call(evalPrm, _EvalMethod, Expression.PropertyOrField(exprPrm, nameof(expression.Body)), innerPrmsPrm);
            if (expression.ReturnType == typeof(void))
                innerBody.Add(Expression.Block(evalCall, Expression.Constant(null)));
            else
                innerBody.Add(Expression.Convert(evalCall, expression.ReturnType));

            var innerLambda = Expression.Lambda(expression.Type, Expression.Block(innerBody), prms);

            outerBody.Add(innerLambda);

            var outerLambda = Expression.Lambda<Func<ExpressionEvaluator, List<Parameter>, LambdaExpression, Delegate>>(
                Expression.Block(new[] { initialPrmsCountVar, innerPrmsPrm }, outerBody),
                evalPrm,
                prmsPrm,
                exprPrm);

            var result = outerLambda.Compile();

            lock (_cache)
            {
                _cache[expression.Type] = result;
            }

            return result(evaluator, parameters, expression);
        }
    }
}
