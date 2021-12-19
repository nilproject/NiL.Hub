using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace UnitTests.Expressions
{
    [TestClass]
    public sealed class ExpressionsSerializationTests
    {
        private sealed class MyCustomType
        {
            public static int Sum(int a, int b)
            {
                return a + b;
            }

            public static T GetValue<T>(T value)
            {
                return value;
            }
        }

        // https://docs.microsoft.com/ru-ru/dotnet/api/system.linq.expressions.expressiontype?f1url=%3FappId%3DDev16IDEF1%26l%3DRU-RU%26k%3Dk(System.Linq.Expressions.ExpressionType);k(DevLang-csharp)%26rd%3Dtrue&view=netcore-3.1
        /*
 Add = 0, AddChecked = 1, And = 2, AndAlso = 3, ArrayLength = 4, ArrayIndex = 5, Call = 6, Coalesce = 7, Conditional = 8, Constant = 9, Convert = 10, ConvertChecked = 11,
 Divide = 12, Equal = 13, ExclusiveOr = 14, GreaterThan = 15, GreaterThanOrEqual = 16, Invoke = 17, Lambda = 18, LeftShift = 19, LessThan = 20, LessThanOrEqual = 21,
 ListInit = 22, MemberAccess = 23, MemberInit = 24, Modulo = 25, Multiply = 26, MultiplyChecked = 27, Negate = 28, UnaryPlus = 29, NegateChecked = 30, New = 31,
 NewArrayInit = 32, NewArrayBounds = 33, Not = 34, NotEqual = 35, Or = 36, OrElse = 37, Parameter = 38, Power = 39, Quote = 40, RightShift = 41, Subtract = 42,
 SubtractChecked = 43, TypeAs = 44, TypeIs = 45, Assign = 46, Block = 47, DebugInfo = 48, Decrement = 49, Dynamic = 50, Default = 51, Extension = 52, Goto = 53, Increment = 54,
 Index = 55, Label = 56, RuntimeVariables = 57, Loop = 58, Switch = 59, Throw = 60, Try = 61, Unbox = 62, AddAssign = 63, AndAssign = 64, DivideAssign = 65, ExclusiveOrAssign = 66,
 LeftShiftAssign = 67, ModuloAssign = 68, MultiplyAssign = 69, OrAssign = 70, PowerAssign = 71, RightShiftAssign = 72, SubtractAssign = 73, AddAssignChecked = 74,
 MultiplyAssignChecked = 75, SubtractAssignChecked = 76, PreIncrementAssign = 77, PreDecrementAssign = 78, PostIncrementAssign = 79, PostDecrementAssign = 80, TypeEqual = 81,
 OnesComplement = 82, IsTrue = 83, IsFalse = 84
        */

        [TestMethod]
        public void Constant_Byte()
            => checkSerialization(Expression.Constant((byte)1));

        [TestMethod]
        public void Constant_SByte()
            => checkSerialization(Expression.Constant((sbyte)1));

        [TestMethod]
        public void Constant_Int16()
            => checkSerialization(Expression.Constant((short)1));

        [TestMethod]
        public void Constant_UInt16()
            => checkSerialization(Expression.Constant((ushort)0x1234));

        [TestMethod]
        public void Constant_Char()
            => checkSerialization(Expression.Constant('a'));

        [TestMethod]
        public void Constant_Int32()
            => checkSerialization(Expression.Constant(1));

        [TestMethod]
        public void Constant_UInt32()
            => checkSerialization(Expression.Constant((uint)0x1234_5678));

        [TestMethod]
        public void Constant_Int64()
            => checkSerialization(Expression.Constant((ulong)0x123_0000_0321));

        [TestMethod]
        public void Constant_UInt64()
            => checkSerialization(Expression.Constant((ulong)0x123_0000_0321));

        [TestMethod]
        public void Constant_Boolean()
            => checkSerialization(Expression.Constant(true));

        [TestMethod]
        public void Constant_String()
            => checkSerialization(Expression.Constant("hello"));

        [TestMethod]
        public void Constant_Float()
            => checkSerialization(Expression.Constant(123f));

        [TestMethod]
        public void Constant_Double()
            => checkSerialization(Expression.Constant(123d));

        [TestMethod]
        public void Constant_Decimal()
            => checkSerialization(Expression.Constant(123m));

        [TestMethod]
        public void Constant_DateTime()
            => checkSerialization(Expression.Constant(DateTime.Now));

        [TestMethod]
        public void Constant_ByteArray()
            => checkSerialization(Expression.Constant(new byte[] { 1, 2, 3, 4, 5 }));

        [TestMethod]
        public void Add()
            => checkSerialization(Expression.Add(Expression.Constant(1), Expression.Constant(2)));

        [TestMethod]
        public void AddChecked()
            => checkSerialization(Expression.AddChecked(Expression.Constant(1), Expression.Constant(2)));

        [TestMethod]
        public void Subtract()
            => checkSerialization(Expression.Subtract(Expression.Constant(1), Expression.Constant(2)));

        [TestMethod]
        public void SubtractChecked()
            => checkSerialization(Expression.SubtractChecked(Expression.Constant(1), Expression.Constant(2)));

        [TestMethod]
        public void Divide()
            => checkSerialization(Expression.Divide(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void Unbox()
            => checkSerialization(Expression.Unbox(Expression.Constant(3, typeof(object)), typeof(int)));

        [TestMethod]
        public void Equal()
            => checkSerialization(Expression.Equal(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void IsTrue()
            => checkSerialization(Expression.IsTrue(Expression.Constant(true)));

        [TestMethod]
        public void IsFalse()
            => checkSerialization(Expression.IsFalse(Expression.Constant(true)));

        [TestMethod]
        public void OnesComplement()
            => checkSerialization(Expression.OnesComplement(Expression.Constant(123)));

        [TestMethod]
        public void ExclusiveOr()
            => checkSerialization(Expression.ExclusiveOr(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void GreaterThan()
            => checkSerialization(Expression.GreaterThan(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void GreaterThanOrEqual()
            => checkSerialization(Expression.GreaterThanOrEqual(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void LessThan()
            => checkSerialization(Expression.LessThan(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void LessThanOrEqual()
            => checkSerialization(Expression.LessThanOrEqual(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void NotEqual()
            => checkSerialization(Expression.NotEqual(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void Modulo()
            => checkSerialization(Expression.Modulo(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void Multiply()
            => checkSerialization(Expression.Multiply(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void MultiplyChecked()
            => checkSerialization(Expression.MultiplyChecked(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void Power()
            => checkSerialization(Expression.Power(Expression.Constant(3d), Expression.Constant(2d)));

        [TestMethod]
        public void LeftShift()
            => checkSerialization(Expression.LeftShift(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void RightShift()
            => checkSerialization(Expression.RightShift(Expression.Constant(3), Expression.Constant(2)));

        [TestMethod]
        public void And()
            => checkSerialization(Expression.And(Expression.Constant(true), Expression.Constant(false)));

        [TestMethod]
        public void AndAlso()
            => checkSerialization(Expression.AndAlso(Expression.Constant(true), Expression.Constant(false)));

        [TestMethod]
        public void AndAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.AndAssign(prm, Expression.Constant(3)), prm);
        }

        [TestMethod]
        public void PowerAssign()
        {
            var prm = Expression.Parameter(typeof(double));
            checkSerialization(Expression.PowerAssign(prm, Expression.Constant(2d)), prm);
        }

        [TestMethod]
        public void Quote()
        {
            Expression<Func<int, Expression<Func<int>>>> expr = x => () => x;
            checkSerialization(expr);
        }

        [TestMethod]
        public void PostIncrementAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.PostIncrementAssign(prm), prm);
        }

        [TestMethod]
        public void PreIncrementAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.PreIncrementAssign(prm), prm);
        }

        [TestMethod]
        public void PostDecrementAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.PostDecrementAssign(prm), prm);
        }

        [TestMethod]
        public void PreDecrementAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.PreDecrementAssign(prm), prm);
        }

        [TestMethod]
        public void MultiplyAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.MultiplyAssign(prm, Expression.Constant(2)), prm);
        }

        [TestMethod]
        public void MultiplyAssignChecked()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.MultiplyAssignChecked(prm, Expression.Constant(2)), prm);
        }

        [TestMethod]
        public void LeftShiftAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.LeftShiftAssign(prm, Expression.Constant(2)), prm);
        }

        [TestMethod]
        public void RightShiftAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.RightShiftAssign(prm, Expression.Constant(2)), prm);
        }

        [TestMethod]
        public void DivideAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.DivideAssign(prm, Expression.Constant(2)), prm);
        }

        [TestMethod]
        public void AddAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.AddAssign(prm, Expression.Constant(3)), prm);
        }

        [TestMethod]
        public void AddAssignChecked()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.AddAssignChecked(prm, Expression.Constant(3)), prm);
        }

        [TestMethod]
        public void SubtractAssign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.SubtractAssign(prm, Expression.Constant(3)), prm);
        }

        [TestMethod]
        public void SubtractAssignChecked()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.SubtractAssignChecked(prm, Expression.Constant(3)), prm);
        }

        [TestMethod]
        public void Assign()
        {
            var prm = Expression.Parameter(typeof(int));
            checkSerialization(Expression.Assign(prm, Expression.Constant(3)), prm);
        }

        [TestMethod]
        public void ArrayLength()
            => checkSerialization(Expression.ArrayLength(Expression.Constant(new[] { 1, 2, 3 })));

        [TestMethod]
        public void Negate()
            => checkSerialization(Expression.Negate(Expression.Constant(1)));

        [TestMethod]
        public void NegateChecked()
            => checkSerialization(Expression.NegateChecked(Expression.Constant(1)));

        [TestMethod]
        public void Not()
            => checkSerialization(Expression.Not(Expression.Constant(true)));

        [TestMethod]
        public void ArrayIndex()
            => checkSerialization(Expression.ArrayIndex(Expression.Constant(new[] { 1, 2, 3 }), Expression.Constant(3)));

        [TestMethod]
        public void ArrayAccess()
            => checkSerialization(Expression.ArrayAccess(Expression.Constant(new[] { 1, 2, 3 }), Expression.Constant(3)));

        [TestMethod]
        public void Call()
            => checkSerialization(
                Expression.Call(
                    Expression.Constant("123"),
                    typeof(string).GetMethods().Where(x => x.Name == "Trim").Skip(1).First(),
                    Expression.Constant('1')));

        [TestMethod]
        public void Invoke()
        {
            Expression<Func<int, int>> expr = x => x;
            checkSerialization(
                  Expression.Invoke(
                      expr,
                      Expression.Constant(1)));
        }

        [TestMethod]
        public void NewArrayBounds()
        {
            Expression<Func<int, int[,]>> expr = x => new int[3, 2];
            checkSerialization(expr.Body, expr.Parameters.ToArray());
        }

        [TestMethod]
        public void MemberAccess()
        {
            var parameter = Expression.Variable(typeof(string));
            checkSerialization(
                  Expression.MakeMemberAccess(
                      parameter,
                      typeof(string).GetProperty(nameof(string.Length))),
                  parameter);
        }

        [TestMethod]
        public void Coalesce()
            => checkSerialization(Expression.Coalesce(Expression.Constant(null), Expression.Constant(new[] { "a" })));

        [TestMethod]
        public void Conditional()
            => checkSerialization(Expression.Condition(
                Expression.Constant(true),
                Expression.Constant("a"),
                Expression.Constant("b")));

        [TestMethod]
        public void CallingMethodOfCustomType()
        {
            Expression<Func<int>> lambda = () => MyCustomType.Sum(1, 2);
            var expression = lambda.Body;

            var typeMap = new TypesMapLayer
            {
                { typeof(MyCustomType), 333 }
            };

            var serializer = new ExpressionSerializer(typeMap);
            var deserializer = new ExpressionDeserializer(typeMap);

            var serialized = serializer.Serialize(expression);
            var deserialized = deserializer.Deserialize(serialized);

            Assert.IsInstanceOfType(deserialized, expression.GetType());
            Assert.AreEqual(expression.ToString(), deserialized.ToString());
        }

        [TestMethod]
        public void GenericParametrization()
        {
            Expression<Func<int>> lambda = () => MyCustomType.GetValue(777);

            var serializer = new ExpressionSerializer();
            var deserializer = new ExpressionDeserializer();

            var serialized = serializer.Serialize(lambda);
            var deserialized = deserializer.Deserialize(serialized);

            Assert.IsInstanceOfType(deserialized, lambda.GetType());
            Assert.AreEqual(lambda.ToString(), deserialized.ToString());
        }

        [TestMethod]
        public void ValueTuples()
        {
            Expression<Func<(int, string)>> expression = () => new ValueTuple<int, string>(1, "23");

            checkSerialization(expression.Body);
        }

        [TestMethod]
        public void ValueTuplesAsObject()
        {
            Expression<Func<object>> expression = () => Convert.ChangeType(new ValueTuple<int, string>(1, "23"), typeof(ValueTuple));

            checkSerialization(expression.Body);
        }

        private static void checkSerialization(ConstantExpression expression)
        {
            var serializer = new ExpressionSerializer();
            var deserializer = new ExpressionDeserializer();
            var serialized = serializer.Serialize(expression);
            var deserialized = deserializer.Deserialize(serialized);

            Assert.IsInstanceOfType(deserialized, expression.GetType());
            var desValue = ((ConstantExpression)deserialized).Value;
            var value = expression.Value;
            if (desValue is Array real)
            {
                var exp = (Array)value;
                Assert.AreEqual(exp.Length, real.Length);

                for (var i = 0; i < exp.Length; i++)
                    Assert.AreEqual(exp.GetValue(i), real.GetValue(i));
            }
            else
                Assert.AreEqual(value, desValue);
        }

        private static void checkSerialization(Expression expression, params ParameterExpression[] parameters)
        {
            var serializer = new ExpressionSerializer();
            var deserializer = new ExpressionDeserializer();
            var serialized = serializer.Serialize(expression, parameters);
            var deserialized = deserializer.Deserialize(serialized, parameters);

            Assert.IsInstanceOfType(deserialized, expression.GetType());
            Assert.AreEqual(expression.ToString(), deserialized.ToString());
        }
    }
}
