using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NiL.Exev;

namespace NiL.Hub.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            hubsTest();
            return;

            experiments();
            return;

            var c = 2;
            Expression<Func<string, string, long, long>> exp = (x, y, z) => x.Length + y.Length + z + c;

            var serializer = new ExpressionSerializer();
            var deserializer = new ExpressionDeserializer();
            var serialized = serializer.Serialize(exp);
            var deserialized = (LambdaExpression)deserializer.Deserialize(serialized);

            var value = new ExpressionEvaluator().Eval(deserialized.Body,
                new[] {
                    new Parameter(deserialized.Parameters[0], "a"),
                    new Parameter(deserialized.Parameters[1], "b"),
                    new Parameter(deserialized.Parameters[2], 123L),
                });

            //bench();
        }

        private interface ITest
        {
            int GetValue();
        }

        private class ClassV1 : ITest
        {
            public int GetValue()
            {
                return 1;
            }
        }

        private class ClassV2 : ITest
        {
            public int GetValue()
            {
                return 2;
            }
        }

        private static void hubsTest()
        {
            var hub0 = new Hub("hub 0");
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            var hub1 = new Hub("hub 1") { PathThrough = true };
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            var hub2 = new Hub("hub 2") { PathThrough = true };
            var hub2EndPoint = new IPEndPoint(IPAddress.Loopback, 4502);
            hub2.StartListening(hub2EndPoint);

            Task.WaitAll(hub1.Connect(hub0EndPoint),
                         hub2.Connect(hub1EndPoint));

            Thread.Sleep(100);

            var hub3 = new Hub("hub 3") { PathThrough = true };
            var hub3EndPoint = new IPEndPoint(IPAddress.Loopback, 4503);
            //hub3.Listen(hub3EndPoint);

            Task.WaitAll(hub3.Connect(hub1EndPoint));

            Task.WaitAll(
                hub1.RegisterInterface<ITest>(new ClassV1(), 1),
                hub2.RegisterInterface<ITest>(new ClassV2(), 2));

            var v = hub0.Get<ITest>().Call(x => x.GetValue(), 2).Result;
            Console.WriteLine(v);
        }

        struct Struct
        {
            public int Field;
        }

        private static void experiments()
        {
            var testObject = new List<int> { 1 };

            Expression<Func<List<int>, int>> testExp = x => x.Count;

            var property = ((PropertyInfo)((MemberExpression)testExp.Body).Member);
            var parameter = Expression.Variable(typeof(object), "x");
            var callExp = (Expression)Expression.MakeMemberAccess(Expression.Convert(parameter, property.DeclaringType), property);
            if (callExp.Type.IsValueType)
                callExp = Expression.Convert(callExp, typeof(object));
            var expr = Expression.Lambda<Func<object, object>>(callExp, parameter);
            expr.Reduce();
            var lambda = expr.Compile();

            var serializer = new ExpressionSerializer();

            var s = new Struct();
            var bs = (object)s;
            var fieldInfo = typeof(Struct).GetField(nameof(Struct.Field));

            var fieldAccess = (Expression)Expression.MakeMemberAccess(Expression.Convert(parameter, typeof(Struct)), fieldInfo);
            if (fieldAccess.Type.IsValueType)
                fieldAccess = Expression.Convert(fieldAccess, typeof(object));
            expr = Expression.Lambda<Func<object, object>>(fieldAccess, parameter);
            expr.Reduce();
            var lambda2 = expr.Compile();

            var ctor = typeof(int[]).GetConstructor(new[] { typeof(int) });

            var intPrm = Expression.Parameter(typeof(int));
            var ctorLambda = Expression.Lambda<Func<int, object>>(Expression.New(ctor, intPrm), intPrm).Compile();

            var lambdaInvokeMethod = lambda.GetType().GetMethod(nameof(lambda.Invoke));
            var targetParameter = Expression.Variable(typeof(object), "y");
            var lambda3 = Expression.Lambda<Func<object, object, object>>(
                Expression.Call(Expression.Convert(targetParameter, lambdaInvokeMethod.DeclaringType), lambdaInvokeMethod, parameter), targetParameter, parameter).Compile();

            var sw = Stopwatch.StartNew();
            var c = 0L;
            for (var i = 0; i < 100_000_000; i++)
            {
                //typeof(int[]).GetElementType();
                //ctorLambda(10);
                //ctor.Invoke(new object[] { 10 });
                //Activator.CreateInstance(typeof(int[]), 10);
                //Array.CreateInstance(typeof(int), 10);
                //_ = new int[10];
                //c += (int)lambda2(bs);
                //c += (int)fieldInfo.GetValue(bs);

                c += (int)lambda3(lambda, testObject);
                //c += (int)lambda(testObject);

                //c += (int)lambda.DynamicInvoke(testObject);
                //c += testObject.Count;
                //c += (int)((PropertyInfo)((MemberExpression)testExp.Body).Member).GetValue(testObject);
            }
            sw.Stop();
            if (c == 0)
                Console.WriteLine("something gone wrong");
            Console.WriteLine(sw.Elapsed);
        }

        private static void bench()
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 5_000_000; i++)
            {
                //Expression<Func<string, string, int>> exp = (x, y) => x.Length + y.Length;
                //exp.Compile()("a", "b");

                //Func<string, string, int> foo = (x, y) => x.Length + y.Length;
                //foo("a", "b");

                //new Context().Eval("(x,y) => x.length + y.length").As<Function>().Call(new Arguments { "a", "b" });

                Expression<Func<string, string, long>> exp = (x, y) => x.Length + y.Length;
                var serializer = new ExpressionSerializer();
                var deserializer = new ExpressionDeserializer();
                var serialized = serializer.Serialize(exp);
                var deserialized = (LambdaExpression)deserializer.Deserialize(serialized);
                var value = new ExpressionEvaluator().Eval(deserialized.Body,
                new[] {
                    new Parameter(deserialized.Parameters[0], "a"),
                    new Parameter(deserialized.Parameters[1], "b"),
                });
            }

            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }

        private static string translateExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Lambda:
                {
                    var lambda = (LambdaExpression)expression;
                    return translateExpression(lambda.Body);
                }

                case ExpressionType.ConvertChecked:
                case ExpressionType.Convert:
                {
                    var type = expression.GetType();
                    var binary = (UnaryExpression)expression;
                    //var left = translateExpression(binary.Left);
                    //var right = translateExpression(binary.Right);
                    break;
                }

                case ExpressionType.Add:
                {
                    var binary = (BinaryExpression)expression;
                    var left = translateExpression(binary.Left);
                    var right = translateExpression(binary.Right);
                    break;
                }

                throw new NotImplementedException();
            }

            return string.Empty;
        }
    }
}
