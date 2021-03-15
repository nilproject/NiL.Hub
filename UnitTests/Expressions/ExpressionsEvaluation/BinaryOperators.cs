using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions.ExpressionsEvaluation
{
    [TestClass]
    public sealed class BinaryOperators
    {
        private void addition<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Add(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void subtract<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Subtract(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void multiply<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Multiply(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void divide<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Divide(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void exclusiveOr<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.ExclusiveOr(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void or<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Or(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void and<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.And(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void modulo<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Modulo(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void greaterThan<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.GreaterThan(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void greaterThanOrEqual<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.GreaterThanOrEqual(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void lessThan<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.LessThan(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void lessThanOrEqual<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.LessThanOrEqual(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void rightShift<T, TResult>(T a, int b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.RightShift(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void leftShift<T, TResult>(T a, int b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.LeftShift(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Addition_Int32() => addition(1, 2, 3);
        [TestMethod]
        public void Addition_UInt32() => addition<uint, uint>(1, 2, 3);
        [TestMethod]
        public void Addition_Int64() => addition<long, long>(1, 2, 3);
        [TestMethod]
        public void Addition_UInt64() => addition<ulong, ulong>(1, 2, 3);
        [TestMethod]
        public void Addition_Int16() => addition<short, int>(1, 2, 3);
        [TestMethod]
        public void Addition_UInt16() => addition<ushort, int>(1, 2, 3);

        [TestMethod]
        public void Subtract_Int32() => subtract(3, 2, 1);
        [TestMethod]
        public void Subtract_UInt32() => subtract<uint, uint>(3, 2, 1);
        [TestMethod]
        public void Subtract_Int64() => subtract<long, long>(3, 2, 1);
        [TestMethod]
        public void Subtract_UInt64() => subtract<ulong, ulong>(3, 2, 1);
        [TestMethod]
        public void Subtract_Int16() => subtract<short, int>(3, 2, 1);
        [TestMethod]
        public void Subtract_UInt16() => subtract<ushort, int>(3, 2, 1);

        [TestMethod]
        public void Multiply_Int32() => multiply(2, 3, 6);
        [TestMethod]
        public void Multiply_UInt32() => multiply<uint, uint>(2, 3, 6);
        [TestMethod]
        public void Multiply_Int64() => multiply<long, long>(2, 3, 6);
        [TestMethod]
        public void Multiply_UInt64() => multiply<ulong, ulong>(2, 3, 6);
        [TestMethod]
        public void Multiply_Int16() => multiply<short, int>(2, 3, 6);
        [TestMethod]
        public void Multiply_UInt16() => multiply<ushort, int>(2, 3, 6);

        [TestMethod]
        public void Divide_Int32() => divide(6, 3, 2);
        [TestMethod]
        public void Divide_UInt32() => divide<uint, uint>(6, 3, 2);
        [TestMethod]
        public void Divide_Int64() => divide<long, long>(6, 3, 2);
        [TestMethod]
        public void Divide_UInt64() => divide<ulong, ulong>(6, 3, 2);
        [TestMethod]
        public void Divide_Int16() => divide<short, int>(6, 3, 2);
        [TestMethod]
        public void Divide_UInt16() => divide<ushort, int>(6, 3, 2);

        [TestMethod]
        public void ExclusiveOr_Int32() => exclusiveOr(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_UInt32() => exclusiveOr<uint, uint>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_Int64() => exclusiveOr<long, long>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_UInt64() => exclusiveOr<ulong, ulong>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_Int16() => exclusiveOr<short, int>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_UInt16() => exclusiveOr<ushort, int>(1, 3, 2);

        [TestMethod]
        public void Or_Int32() => or(1, 3, 3);
        [TestMethod]
        public void Or_UInt32() => or<uint, uint>(1, 3, 3);
        [TestMethod]
        public void Or_Int64() => or<long, long>(1, 3, 3);
        [TestMethod]
        public void Or_UInt64() => or<ulong, ulong>(1, 3, 3);
        [TestMethod]
        public void Or_Int16() => or<short, int>(1, 3, 3);
        [TestMethod]
        public void Or_UInt16() => or<ushort, int>(1, 3, 3);

        [TestMethod]
        public void And_Int32() => and(13, 11, 9);
        [TestMethod]
        public void And_UInt32() => and<uint, uint>(13, 11, 9);
        [TestMethod]
        public void And_Int64() => and<long, long>(13, 11, 9);
        [TestMethod]
        public void And_UInt64() => and<ulong, ulong>(13, 11, 9);
        [TestMethod]
        public void And_Int16() => and<short, int>(13, 11, 9);
        [TestMethod]
        public void And_UInt16() => and<ushort, int>(13, 11, 9);

        [TestMethod]
        public void Modulo_Int32() => modulo(8, 3, 2);
        [TestMethod]
        public void Modulo_UInt32() => modulo<uint, uint>(8, 3, 2);
        [TestMethod]
        public void Modulo_Int64() => modulo<long, long>(8, 3, 2);
        [TestMethod]
        public void Modulo_UInt64() => modulo<ulong, ulong>(8, 3, 2);
        [TestMethod]
        public void Modulo_Int16() => modulo<short, int>(8, 3, 2);
        [TestMethod]
        public void Modulo_UInt16() => modulo<ushort, int>(8, 3, 2);

        [TestMethod]
        public void GreaterThan_Int32() => greaterThan(8, 3, true);
        [TestMethod]
        public void GreaterThan_UInt32() => greaterThan<uint, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_Int64() => greaterThan<long, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_UInt64() => greaterThan<ulong, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_Int16() => greaterThan<short, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_UInt16() => greaterThan<ushort, bool>(8, 3, true);

        [TestMethod]
        public void GreaterThanOrEqual_Int32() => greaterThanOrEqual(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_UInt32() => greaterThanOrEqual<uint, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_Int64() => greaterThanOrEqual<long, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_UInt64() => greaterThanOrEqual<ulong, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_Int16() => greaterThanOrEqual<short, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_UInt16() => greaterThanOrEqual<ushort, bool>(8, 3, true);

        [TestMethod]
        public void LessThan_Int32() => lessThan(8, 3, false);
        [TestMethod]
        public void LessThan_UInt32() => lessThan<uint, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_Int64() => lessThan<long, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_UInt64() => lessThan<ulong, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_Int16() => lessThan<short, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_UInt16() => lessThan<ushort, bool>(8, 3, false);

        [TestMethod]
        public void LessThanOrEqual_Int32() => lessThanOrEqual(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_UInt32() => lessThanOrEqual<uint, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_Int64() => lessThanOrEqual<long, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_UInt64() => lessThanOrEqual<ulong, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_Int16() => lessThanOrEqual<short, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_UInt16() => lessThanOrEqual<ushort, bool>(8, 3, false);

        [TestMethod]
        public void RightShift_Int32() => rightShift(8, 3, 1);
        [TestMethod]
        public void RightShift_UInt32() => rightShift<uint, uint>(8, 3, 1);
        [TestMethod]
        public void RightShift_Int64() => rightShift<long, long>(8, 3, 1);
        [TestMethod]
        public void RightShift_UInt64() => rightShift<ulong, ulong>(8, 3, 1);
        [TestMethod]
        public void RightShift_Int16() => rightShift<short, int>(8, 3, 1);
        [TestMethod]
        public void RightShift_UInt16() => rightShift<ushort, int>(8, 3, 1);

        [TestMethod]
        public void LeftShift_Int32() => leftShift(8, 3, 64);
        [TestMethod]
        public void LeftShift_UInt32() => leftShift<uint, uint>(8, 3, 64);
        [TestMethod]
        public void LeftShift_Int64() => leftShift<long, long>(8, 3, 64);
        [TestMethod]
        public void LeftShift_UInt64() => leftShift<ulong, ulong>(8, 3, 64);
        [TestMethod]
        public void LeftShift_Int16() => leftShift<short, int>(8, 3, 64);
        [TestMethod]
        public void LeftShift_UInt16() => leftShift<ushort, int>(8, 3, 64);
    }
}
