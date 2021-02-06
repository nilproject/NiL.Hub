using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions.ExpressionsEvaluation
{
    [TestClass]
    public sealed class BinaryOperators
    {
        private void Addition<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Add(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void Subtract<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Subtract(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void Multiply<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Multiply(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void Divide<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Divide(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void ExclusiveOr<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.ExclusiveOr(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void Or<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Or(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void And<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.And(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void Modulo<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.Modulo(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void GreaterThan<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.GreaterThan(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void GreaterThanOrEqual<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.GreaterThanOrEqual(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void LessThan<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.LessThan(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void LessThanOrEqual<T, TResult>(T a, T b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.LessThanOrEqual(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void RightShift<T, TResult>(T a, int b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.RightShift(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        private void LeftShift<T, TResult>(T a, int b, TResult expected) where T : struct
        {
            var evaluator = new ExpressionEvaluator();
            var exp = Expression.LeftShift(Expression.Constant(a), Expression.Constant(b));
            var result = (TResult)evaluator.Eval(exp);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Addition_Int32() => Addition(1, 2, 3);
        [TestMethod]
        public void Addition_UInt32() => Addition<uint, uint>(1, 2, 3);
        [TestMethod]
        public void Addition_Int64() => Addition<long, long>(1, 2, 3);
        [TestMethod]
        public void Addition_UInt64() => Addition<ulong, ulong>(1, 2, 3);
        [TestMethod]
        public void Addition_Int16() => Addition<short, int>(1, 2, 3);
        [TestMethod]
        public void Addition_UInt16() => Addition<ushort, int>(1, 2, 3);

        [TestMethod]
        public void Subtract_Int32() => Subtract(3, 2, 1);
        [TestMethod]
        public void Subtract_UInt32() => Subtract<uint, uint>(3, 2, 1);
        [TestMethod]
        public void Subtract_Int64() => Subtract<long, long>(3, 2, 1);
        [TestMethod]
        public void Subtract_UInt64() => Subtract<ulong, ulong>(3, 2, 1);
        [TestMethod]
        public void Subtract_Int16() => Subtract<short, int>(3, 2, 1);
        [TestMethod]
        public void Subtract_UInt16() => Subtract<ushort, int>(3, 2, 1);

        [TestMethod]
        public void Multiply_Int32() => Multiply(2, 3, 6);
        [TestMethod]
        public void Multiply_UInt32() => Multiply<uint, uint>(2, 3, 6);
        [TestMethod]
        public void Multiply_Int64() => Multiply<long, long>(2, 3, 6);
        [TestMethod]
        public void Multiply_UInt64() => Multiply<ulong, ulong>(2, 3, 6);
        [TestMethod]
        public void Multiply_Int16() => Multiply<short, int>(2, 3, 6);
        [TestMethod]
        public void Multiply_UInt16() => Multiply<ushort, int>(2, 3, 6);

        [TestMethod]
        public void Divide_Int32() => Divide(6, 3, 2);
        [TestMethod]
        public void Divide_UInt32() => Divide<uint, uint>(6, 3, 2);
        [TestMethod]
        public void Divide_Int64() => Divide<long, long>(6, 3, 2);
        [TestMethod]
        public void Divide_UInt64() => Divide<ulong, ulong>(6, 3, 2);
        [TestMethod]
        public void Divide_Int16() => Divide<short, int>(6, 3, 2);
        [TestMethod]
        public void Divide_UInt16() => Divide<ushort, int>(6, 3, 2);

        [TestMethod]
        public void ExclusiveOr_Int32() => ExclusiveOr(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_UInt32() => ExclusiveOr<uint, uint>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_Int64() => ExclusiveOr<long, long>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_UInt64() => ExclusiveOr<ulong, ulong>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_Int16() => ExclusiveOr<short, int>(1, 3, 2);
        [TestMethod]
        public void ExclusiveOr_UInt16() => ExclusiveOr<ushort, int>(1, 3, 2);

        [TestMethod]
        public void Or_Int32() => Or(1, 3, 3);
        [TestMethod]
        public void Or_UInt32() => Or<uint, uint>(1, 3, 3);
        [TestMethod]
        public void Or_Int64() => Or<long, long>(1, 3, 3);
        [TestMethod]
        public void Or_UInt64() => Or<ulong, ulong>(1, 3, 3);
        [TestMethod]
        public void Or_Int16() => Or<short, int>(1, 3, 3);
        [TestMethod]
        public void Or_UInt16() => Or<ushort, int>(1, 3, 3);

        [TestMethod]
        public void And_Int32() => And(13, 11, 9);
        [TestMethod]
        public void And_UInt32() => And<uint, uint>(13, 11, 9);
        [TestMethod]
        public void And_Int64() => And<long, long>(13, 11, 9);
        [TestMethod]
        public void And_UInt64() => And<ulong, ulong>(13, 11, 9);
        [TestMethod]
        public void And_Int16() => And<short, int>(13, 11, 9);
        [TestMethod]
        public void And_UInt16() => And<ushort, int>(13, 11, 9);

        [TestMethod]
        public void Modulo_Int32() => Modulo(8, 3, 2);
        [TestMethod]
        public void Modulo_UInt32() => Modulo<uint, uint>(8, 3, 2);
        [TestMethod]
        public void Modulo_Int64() => Modulo<long, long>(8, 3, 2);
        [TestMethod]
        public void Modulo_UInt64() => Modulo<ulong, ulong>(8, 3, 2);
        [TestMethod]
        public void Modulo_Int16() => Modulo<short, int>(8, 3, 2);
        [TestMethod]
        public void Modulo_UInt16() => Modulo<ushort, int>(8, 3, 2);

        [TestMethod]
        public void GreaterThan_Int32() => GreaterThan(8, 3, true);
        [TestMethod]
        public void GreaterThan_UInt32() => GreaterThan<uint, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_Int64() => GreaterThan<long, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_UInt64() => GreaterThan<ulong, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_Int16() => GreaterThan<short, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThan_UInt16() => GreaterThan<ushort, bool>(8, 3, true);

        [TestMethod]
        public void GreaterThanOrEqual_Int32() => GreaterThanOrEqual(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_UInt32() => GreaterThanOrEqual<uint, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_Int64() => GreaterThanOrEqual<long, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_UInt64() => GreaterThanOrEqual<ulong, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_Int16() => GreaterThanOrEqual<short, bool>(8, 3, true);
        [TestMethod]
        public void GreaterThanOrEqual_UInt16() => GreaterThanOrEqual<ushort, bool>(8, 3, true);

        [TestMethod]
        public void LessThan_Int32() => LessThan(8, 3, false);
        [TestMethod]
        public void LessThan_UInt32() => LessThan<uint, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_Int64() => LessThan<long, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_UInt64() => LessThan<ulong, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_Int16() => LessThan<short, bool>(8, 3, false);
        [TestMethod]
        public void LessThan_UInt16() => LessThan<ushort, bool>(8, 3, false);

        [TestMethod]
        public void LessThanOrEqual_Int32() => LessThanOrEqual(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_UInt32() => LessThanOrEqual<uint, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_Int64() => LessThanOrEqual<long, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_UInt64() => LessThanOrEqual<ulong, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_Int16() => LessThanOrEqual<short, bool>(8, 3, false);
        [TestMethod]
        public void LessThanOrEqual_UInt16() => LessThanOrEqual<ushort, bool>(8, 3, false);

        [TestMethod]
        public void RightShift_Int32() => RightShift(8, 3, 1);
        [TestMethod]
        public void RightShift_UInt32() => RightShift<uint, uint>(8, 3, 1);
        [TestMethod]
        public void RightShift_Int64() => RightShift<long, long>(8, 3, 1);
        [TestMethod]
        public void RightShift_UInt64() => RightShift<ulong, ulong>(8, 3, 1);
        [TestMethod]
        public void RightShift_Int16() => RightShift<short, int>(8, 3, 1);
        [TestMethod]
        public void RightShift_UInt16() => RightShift<ushort, int>(8, 3, 1);

        [TestMethod]
        public void LeftShift_Int32() => LeftShift(8, 3, 64);
        [TestMethod]
        public void LeftShift_UInt32() => LeftShift<uint, uint>(8, 3, 64);
        [TestMethod]
        public void LeftShift_Int64() => LeftShift<long, long>(8, 3, 64);
        [TestMethod]
        public void LeftShift_UInt64() => LeftShift<ulong, ulong>(8, 3, 64);
        [TestMethod]
        public void LeftShift_Int16() => LeftShift<short, int>(8, 3, 64);
        [TestMethod]
        public void LeftShift_UInt16() => LeftShift<ushort, int>(8, 3, 64);
    }
}
