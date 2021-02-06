using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Bson;
using NiL.Hub;

namespace Tests.Hubs
{
    [TestClass]
    public sealed class HubChaining
    {
        [TestInitialize] public void Init() => AppDomain.CurrentDomain.FirstChanceException += currentDomain_FirstChanceException;
        [TestCleanup] public void Cleanup() => AppDomain.CurrentDomain.FirstChanceException -= currentDomain_FirstChanceException;
        private void currentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e) { }

        [TestMethod]
        public void SimpleChain()
        {
            using var hub0 = new Hub("hub 0");
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            using var hub2 = new Hub("hub 2");

            Task.WaitAll(hub1.Connect(hub0EndPoint),
                         hub2.Connect(hub1EndPoint));

            var hub0KnownHubs = hub0.KnownHubs.ToArray();
            Assert.AreEqual(2, hub0KnownHubs.Length);
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 1"));
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 2"));

            var hub1KnownHubs = hub1.KnownHubs.ToArray();
            Assert.AreEqual(2, hub1KnownHubs.Length);
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 2"));

            var hub2KnownHubs = hub2.KnownHubs.ToArray();
            Assert.AreEqual(2, hub1KnownHubs.Length);
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 1"));
        }

        [TestMethod]
        public void ConnectInTheMiddleOfChain()
        {
            using var hub0 = new Hub("hub 0");
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            using var hub2 = new Hub("hub 2");

            Task.WaitAll(hub1.Connect(hub0EndPoint),
                         hub2.Connect(hub1EndPoint));

            using var hub3 = new Hub("hub 3");
            hub3.Connect(hub1EndPoint).Wait();

            var hub0KnownHubs = hub0.KnownHubs.ToArray();
            Assert.AreEqual(3, hub0KnownHubs.Length);
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 1"));
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 2"));
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 3"));

            var hub1KnownHubs = hub1.KnownHubs.ToArray();
            Assert.AreEqual(3, hub1KnownHubs.Length);
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 2"));
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 3"));

            var hub2KnownHubs = hub2.KnownHubs.ToArray();
            Assert.AreEqual(3, hub2KnownHubs.Length);
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 1"));
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 3"));

            var hub3KnownHubs = hub3.KnownHubs.ToArray();
            Assert.AreEqual(3, hub3KnownHubs.Length);
            Assert.IsTrue(hub3KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub3KnownHubs.Any(x => x.Name == "hub 1"));
            Assert.IsTrue(hub3KnownHubs.Any(x => x.Name == "hub 2"));
        }

        [TestMethod]
        public void DisnnectFromTheMiddleOfChain()
        {
            using var hub0 = new Hub("hub 0");
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            using var hub2 = new Hub("hub 2");

            Task.WaitAll(hub1.Connect(hub0EndPoint),
                         hub2.Connect(hub1EndPoint));

            using var hub3 = new Hub("hub 3");
            hub3.Connect(hub1EndPoint).Wait();
            hub3.Connections.First().Disconnect();

            Thread.Sleep(10);

            var hub0KnownHubs = hub0.KnownHubs.ToArray();
            Assert.AreEqual(2, hub0KnownHubs.Length);
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 1"));
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 2"));

            var hub1KnownHubs = hub1.KnownHubs.ToArray();
            Assert.AreEqual(2, hub1KnownHubs.Length);
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 2"));

            var hub2KnownHubs = hub2.KnownHubs.ToArray();
            Assert.AreEqual(2, hub2KnownHubs.Length);
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 1"));

            var hub3KnownHubs = hub3.KnownHubs.ToArray();
            Assert.AreEqual(0, hub3KnownHubs.Length);
        }

        [TestMethod]
        public void TwoConnections()
        {
            using var hub0 = new Hub("hub 0");
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");

            Task.WaitAll(hub1.Connect(hub0EndPoint),
                         hub1.Connect(hub0EndPoint));

            var hub0KnownHubs = hub0.KnownHubs.ToArray();
            Assert.AreEqual(1, hub0KnownHubs.Length);
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 1"));

            var hub1KnownHubs = hub1.KnownHubs.ToArray();
            Assert.AreEqual(1, hub1KnownHubs.Length);
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 0"));

            var connections0 = hub0.Connections.ToArray();
            Assert.AreEqual(2, connections0.Length);
            Assert.IsTrue(connections0.All(x => x.State == HubConnectionState.Active));

            var connections1 = hub1.Connections.ToArray();
            Assert.AreEqual(2, connections1.Length);
            Assert.IsTrue(connections1.All(x => x.State == HubConnectionState.Active));
        }

        [TestMethod]
        public void TwoConnectionsAndYetAnthorHub()
        {
            using var hub0 = new Hub("hub 0");
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");

            Task.WaitAll(hub1.Connect(hub0EndPoint),
                         hub1.Connect(hub0EndPoint));

            using var hub2 = new Hub("hub 2");
            hub2.Connect(hub0EndPoint).Wait();

            var hub0KnownHubs = hub0.KnownHubs.ToArray();
            Assert.AreEqual(2, hub0KnownHubs.Length);
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 1"));
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 2"));

            var hub1KnownHubs = hub1.KnownHubs.ToArray();
            Assert.AreEqual(2, hub1KnownHubs.Length);
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 2"));

            var hub2KnownHubs = hub2.KnownHubs.ToArray();
            Assert.AreEqual(2, hub2KnownHubs.Length);
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 1"));

            var connections0 = hub0.Connections.ToArray();
            Assert.AreEqual(3, connections0.Length);
            Assert.IsTrue(connections0.All(x => x.State == HubConnectionState.Active));

            var connections1 = hub1.Connections.ToArray();
            Assert.AreEqual(2, connections1.Length);
            Assert.IsTrue(connections1.All(x => x.State == HubConnectionState.Active));

            var connections2 = hub2.Connections.ToArray();
            Assert.AreEqual(1, connections2.Length);
            Assert.IsTrue(connections2.All(x => x.State == HubConnectionState.Active));
        }

        [TestMethod]
        public void TwoConnectionsAndYetAnthorHubThenOneDisconnect()
        {
            using var hub0 = new Hub("hub 0");
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");

            Task.WaitAll(hub1.Connect(hub0EndPoint),
                         hub1.Connect(hub0EndPoint));

            using var hub2 = new Hub("hub 2");
            hub2.Connect(hub0EndPoint).Wait();

            hub1.Connections.First().Disconnect();

            Thread.Sleep(10);

            var hub0KnownHubs = hub0.KnownHubs.ToArray();
            Assert.AreEqual(2, hub0KnownHubs.Length);
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 1"));
            Assert.IsTrue(hub0KnownHubs.Any(x => x.Name == "hub 2"));

            var hub1KnownHubs = hub1.KnownHubs.ToArray();
            Assert.AreEqual(2, hub1KnownHubs.Length);
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub1KnownHubs.Any(x => x.Name == "hub 2"));

            var hub2KnownHubs = hub2.KnownHubs.ToArray();
            Assert.AreEqual(2, hub2KnownHubs.Length);
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 0"));
            Assert.IsTrue(hub2KnownHubs.Any(x => x.Name == "hub 1"));

            var connections0 = hub0.Connections.ToArray();
            Assert.AreEqual(2, connections0.Length);
            Assert.IsTrue(connections0.All(x => x.State == HubConnectionState.Active));

            var connections1 = hub1.Connections.ToArray();
            Assert.AreEqual(1, connections1.Length);
            Assert.IsTrue(connections1.All(x => x.State == HubConnectionState.Active));

            var connections2 = hub2.Connections.ToArray();
            Assert.AreEqual(1, connections2.Length);
            Assert.IsTrue(connections2.All(x => x.State == HubConnectionState.Active));
        }
    }
}
