using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions.ExpressionsEvaluation
{
    [TestClass]
    public sealed class CommonOperations
    {
        private sealed class TestClass
        {
            public TestClass(int value0)
            {
                Value0 = value0;
            }

            public int Value0 { get; }
            public int Value1 { get; set; }
        }

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

        [TestMethod]
        public void NewTestClass()
        {
            var evaluator = new ExpressionEvaluator();
            Expression<Func<TestClass>> expression = () => new TestClass(1) { Value1 = 2 };

            var result = (TestClass)evaluator.Eval(expression.Body);

            Assert.AreEqual(2, result.Value1);
        }

        [TestMethod]
        public void AssignmentToProperty()
        {
            var evaluator = new ExpressionEvaluator();
            var prm = Expression.Parameter(typeof(TestClass), "obj");
            var obj = new TestClass(2);
            var expression = Expression.Block(Expression.Assign(Expression.PropertyOrField(prm, nameof(TestClass.Value1)), Expression.Constant(1)));

            var result = evaluator.Eval(expression, new Parameter(prm, obj));

            Assert.AreEqual(1, obj.Value1);
            Assert.AreEqual(1, result);
        }
    }
}
