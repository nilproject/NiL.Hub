using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Hub;

namespace Tests.Hubs
{
    [TestClass]
    public sealed class RemoteCalling
    {
        private interface ITestInterface
        {
            int sum(int a, int b);
        }

        private sealed class TestImplementation : ITestInterface
        {
            public int sum(int a, int b)
            {
                return a + b;
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
            var result = remoteInterface.Call(_ => _.sum(a, b));

            GC.Collect(2);

            result.Wait();

            Assert.AreEqual(3, result.Result);
        }

        [TestMethod]
        [Timeout(1000)]
        public void RemoteCallWithMediator()
        {
            using var hub1 = new Hub(777005, "hub 1");
            var endpoint1 = new IPEndPoint(IPAddress.Loopback, 4500);

            using var hub2 = new Hub(777006, "hub 2");
            var endpoint2 = new IPEndPoint(IPAddress.Loopback, 4501);

            using var hub3 = new Hub(777007, "hub 3");

            hub1.StartListening(endpoint1);
            hub2.Connect(endpoint1).Wait();

            hub2.StartListening(endpoint2);
            hub3.Connect(endpoint2).Wait();

            hub1.RegisterInterface<ITestInterface>(new TestImplementation()).Wait();

            while (!hub3.TryGet<ITestInterface>(out _))
                Thread.Sleep(1);

            var a = 1;
            var b = 2;
            var remoteInterface = hub3.Get<ITestInterface>();
            var result = remoteInterface.Call(_ => _.sum(a, b));

            GC.Collect(2);

            result.Wait();

            Assert.AreEqual(3, result.Result);
        }
    }
}
