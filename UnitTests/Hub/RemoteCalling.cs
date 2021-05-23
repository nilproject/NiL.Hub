using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Hub;

namespace UnitTests.HubTests
{
    [TestClass]
    public sealed class RemoteCalling
    {
        private interface ITestInterface
        {
            int Sum(int a, int b);
            Task<int> GetDelayedValue(int value);
            IEnumerable<int> GetEnumeratedValues();
            void SomeMethod();
            [DenyRemoteCall]
            void DeniedMethod();
            [DenyRemoteCall]
            int DeniedFunction();
            object MethodWithCallback(Func<int, string, object> callback);
        }

        private sealed class TestImplementation : ITestInterface
        {
            private readonly Action _callback;

            public TestImplementation()
                : this(null)
            { }

            public TestImplementation(Action callback)
            {
                _callback = callback;
            }

            public int Sum(int a, int b)
            {
                return a + b;
            }

            public Task<int> GetDelayedValue(int value)
            {
                return Task.Delay(1).ContinueWith(x => value);
            }

            public IEnumerable<int> GetEnumeratedValues()
            {
                _callback();

                yield return 1;
                yield return 2;
                yield return 3;
            }

            public void SomeMethod()
            {
                _callback();
            }

            public void DeniedMethod()
            {
                _callback();
            }

            public int DeniedFunction()
            {
                _callback();
                return 1;
            }

            public object MethodWithCallback(Func<int, string, object> callback)
            {
                return callback(1, "str");
            }
        }

        [TestMethod]
        [Timeout(1000)]
        public void InterfacePropagation()
        {
            using var hub1 = new Hub(777001, "hub 1");
            using var hub2 = new Hub(777002, "hub 2");
            var endpoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub1.StartListening(endpoint);
            hub2.Connect(endpoint).Wait();

            hub1.RegisterInterface<ITestInterface>(new TestImplementation()).Wait();

            while (!hub2.TryGet<ITestInterface>(out _))
                Thread.Sleep(1);

            var remoteInterface = hub2.Get<ITestInterface>();

            Assert.IsNotNull(remoteInterface);
            Assert.AreEqual(typeof(ITestInterface).FullName, remoteInterface.Name);
        }

        [TestMethod]
        [Timeout(1000)]
        public void RemoteCall()
        {
            using var hub1 = new Hub(777003, "hub 1");
            using var hub2 = new Hub(777004, "hub 2");
            var endpoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub1.StartListening(endpoint);
            hub2.Connect(endpoint).Wait();

            hub1.RegisterInterface<ITestInterface>(new TestImplementation()).Wait();

            while (!hub2.TryGet<ITestInterface>(out _))
                Thread.Sleep(1);

            var a = 1;
            var b = 2;
            var remoteInterface = hub2.Get<ITestInterface>();
            var result = remoteInterface.Call(_ => _.Sum(a, b));

            GC.Collect(2);

            result.Wait();

            Assert.AreEqual(3, result.Result);
        }

        [TestMethod]
        //[Timeout(1000)]
        public void RemoteCallWithMediator()
        {
            using var hub1 = new Hub(777005, "hub 1");
            var endpoint1 = new IPEndPoint(IPAddress.Loopback, 4500);

            hub1.RegisterInterface<ITestInterface>(new TestImplementation()).Wait();

            using var hub2 = new Hub(777006, "hub 2");
            hub2.PathThrough = true;
            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);

            using var hub3 = new Hub(777007, "hub 3");

            hub1.StartListening(endpoint1);
            hub2.Connect(endpoint1).Wait();

            while (!hub2.TryGet<ITestInterface>(out _))
                Thread.Sleep(1);

            hub2.StartListening(endpoint2);
            hub3.Connect(endpoint2).Wait();

            while (!hub3.TryGet<ITestInterface>(out _))
                Thread.Sleep(1);

            var a = 1;
            var b = 2;
            var remoteInterface = hub3.Get<ITestInterface>();
            var result = remoteInterface.Call(_ => _.Sum(a, b));

            GC.Collect(2);

            result.Wait();

            Assert.AreEqual(3, result.Result);
        }

        [TestMethod]
        public void CallWithTaskAsResult()
        {
            using var hub1 = new Hub(777008, "hub 1");

            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);
            using var hub2 = new Hub(777009, "hub 2");
            hub2.StartListening(endpoint2);

            hub1.Connect(endpoint2).Wait();

            hub1.RegisterInterface<ITestInterface>(new TestImplementation()).Wait();

            var value = hub2.Get<ITestInterface>().Call(x => x.GetDelayedValue(123));

            value.Wait();

            Assert.AreEqual(123, value.Result);
        }

        [TestMethod]
        public void CallWithEnumerableAsResult()
        {
            using var hub1 = new Hub(777008, "hub 1");

            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);
            using var hub2 = new Hub(777009, "hub 2");
            hub2.StartListening(endpoint2);

            hub1.Connect(endpoint2).Wait();

            var called = false;

            hub1.RegisterInterface<ITestInterface>(new TestImplementation(() => called = true)).Wait();

            var value = hub2.Get<ITestInterface>().Call(x => x.GetEnumeratedValues());

            value.Wait();

            Assert.IsTrue(called, nameof(called));

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, value.Result as ICollection);
        }

        [TestMethod]
        [Timeout(1000)]
        public void CallMethodWithVoidResult()
        {
            using var hub1 = new Hub(777008, "hub 1");

            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);
            using var hub2 = new Hub(777009, "hub 2");
            hub2.StartListening(endpoint2);

            hub1.Connect(endpoint2).Wait();

            var called = false;

            hub1.RegisterInterface<ITestInterface>(new TestImplementation(() => called = true)).Wait();

            var value = hub2.Get<ITestInterface>().Call(x => x.SomeMethod());

            value.Wait();

            Assert.IsTrue(called, nameof(called));
        }

        [TestMethod]
        public void TryToCallDeniedMethod()
        {
            using var hub1 = new Hub(777008, "hub 1");

            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);
            using var hub2 = new Hub(777009, "hub 2");
            hub2.StartListening(endpoint2);

            hub1.Connect(endpoint2).Wait();

            var called = false;

            hub1.RegisterInterface<ITestInterface>(new TestImplementation(() => called = true)).Wait();

            var value = hub2.Get<ITestInterface>().Call(x => x.DeniedMethod());

            try
            {
                value.Wait();
            }
            catch (AggregateException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(RemoteException));
            }

            Assert.IsFalse(called, nameof(called));
        }

        [TestMethod]
        public void TryToCallDeniedMethodWithNonVoidResult()
        {
            using var hub1 = new Hub(777008, "hub 1");

            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);
            using var hub2 = new Hub(777009, "hub 2");
            hub2.StartListening(endpoint2);

            hub1.Connect(endpoint2).Wait();

            var called = false;

            hub1.RegisterInterface<ITestInterface>(new TestImplementation(() => called = true)).Wait();

            var value = hub2.Get<ITestInterface>().Call(x => x.DeniedFunction());

            try
            {
                value.Wait();
            }
            catch (AggregateException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(RemoteException));
            }

            Assert.IsFalse(called, nameof(called));
        }

        [TestMethod]
        public void SendLambda()
        {
            using var hub1 = new Hub(777008, "hub 1");

            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);
            using var hub2 = new Hub(777009, "hub 2");
            hub2.StartListening(endpoint2);

            hub1.Connect(endpoint2).Wait();

            hub1.RegisterInterface<ITestInterface>(new TestImplementation()).Wait();

            var value = hub2.Get<ITestInterface>().Call(x => x.MethodWithCallback((y, s) => s + y));

            value.Wait();

            Assert.AreEqual("str1", value.Result);
        }
    }
}
