using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions.ExpressionsEvaluation
{
    [TestClass]
    public sealed class Invokation
    {
        [TestMethod]
        public void Call_Static()
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Call(
                typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(object), typeof(object) }),
                Expression.Constant("hello, "),
                Expression.Constant("world!"));

            var result = evaluator.Eval(exp);

            Assert.AreEqual("hello, world!", result);
        }

        [TestMethod]
        public void Call_Dynamic()
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Call(
                Expression.Constant("111122222111111"),
                typeof(string).GetMethod(nameof(string.Trim), new[] { typeof(char) }),
                Expression.Constant('1'));

            var result = evaluator.Eval(exp);

            Assert.AreEqual("22222", result);
        }

        [TestMethod]
        public void Invoke()
        {
            //var lambda = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(object), typeof(object) });
            Func<object, object, string> lambda = (x, y) => string.Concat(x, y);
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Invoke(
                Expression.Constant(lambda),
                Expression.Constant("hello, "),
                Expression.Constant("world!"));

            var result = evaluator.Eval(exp);

            Assert.AreEqual("hello, world!", result);
        }
    }
}
