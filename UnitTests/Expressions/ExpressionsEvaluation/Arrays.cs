using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions.ExpressionsEvaluation
{
    [TestClass]
    public sealed class Arrays
    {
        [TestMethod]
        public void ArrayLength()
        {
            var evaluator = new ExpressionEvaluator();
            var parameters = new[] { new Parameter(Expression.Variable(typeof(int[]), "x"), new[] { 1, 2, 3, 4 }) };
            var exp = Expression.ArrayLength(parameters[0].ParameterExpression);

            var result = evaluator.Eval(exp, parameters);

            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void ArrayAccess_Write()
        {
            var evaluator = new ExpressionEvaluator();
            var parameters = new[] { new Parameter(Expression.Variable(typeof(int[]), "x"), new[] { 1, 2, 3, 4 }) };
            var exp = Expression.Assign(Expression.ArrayAccess(parameters[0].ParameterExpression, Expression.Constant(1)), Expression.Constant(777));

            var result = evaluator.Eval(exp, parameters);

            Assert.AreEqual(777, ((int[])parameters[0].Value)[1]);
            Assert.AreEqual(777, result);
        }

        [TestMethod]
        public void ArrayAccess_Read()
        {
            var evaluator = new ExpressionEvaluator();
            var parameters = new[] { new Parameter(Expression.Variable(typeof(int[]), "x"), new[] { 1, 2, 3, 4 }) };
            var exp = Expression.ArrayAccess(parameters[0].ParameterExpression, Expression.Constant(1));

            var result = evaluator.Eval(exp, parameters);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void NewArrayBounds_1dim()
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.NewArrayBounds(typeof(int), Expression.Constant(2));

            var result = evaluator.Eval(exp);

            Assert.IsInstanceOfType(result, typeof(int[]));
            Assert.AreEqual(2, ((int[])result).GetLength(0));
        }

        [TestMethod]
        public void NewArrayBounds_2dim()
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3));

            var result = evaluator.Eval(exp);

            Assert.IsInstanceOfType(result, typeof(int[,]));
            Assert.AreEqual(2, ((int[,])result).GetLength(0));
            Assert.AreEqual(3, ((int[,])result).GetLength(1));
        }
    }
}
