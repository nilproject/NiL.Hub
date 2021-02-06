using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions.ExpressionsEvaluation
{
    [TestClass]
    public sealed class BinaryOperatorsCommon
    {
        [TestMethod]
        public void AddAssign()
        {
            var evaluator = new ExpressionEvaluator();
            var parameters = new[] { new Parameter(Expression.Variable(typeof(int), "x"), 1) };
            var exp = Expression.AddAssign(parameters[0].ParameterExpression, Expression.Constant(2));

            var result = evaluator.Eval(exp, parameters);

            Assert.AreEqual(3, parameters[0].Value);
            Assert.AreEqual(3, result);
        }
    }
}
