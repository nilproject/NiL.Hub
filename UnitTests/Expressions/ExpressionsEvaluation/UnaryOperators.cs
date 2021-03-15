using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions.ExpressionsEvaluation
{
    [TestClass]
    public sealed class UnaryOperators
    {
        [TestMethod]
        public void NewList()
        {
            var evaluator = new ExpressionEvaluator();
            var ctor = typeof(List<int>).GetConstructor(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new[] { typeof(int) }, null);
            var exp = Expression.New(ctor, Expression.Constant(100));

            var result = (List<int>)evaluator.Eval(exp);

            Assert.IsInstanceOfType(result, typeof(List<int>));
            Assert.AreEqual(100, result.Capacity);
        }

        [TestMethod]
        public void NewString()
        {
            var evaluator = new ExpressionEvaluator();
            var ctor = typeof(string).GetConstructor(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new[] { typeof(char), typeof(int) }, null);
            var exp = Expression.New(ctor, Expression.Constant('a'), Expression.Constant(5));

            var result = (string)evaluator.Eval(exp);

            Assert.AreEqual("aaaaa", result);
        }
    }
}
