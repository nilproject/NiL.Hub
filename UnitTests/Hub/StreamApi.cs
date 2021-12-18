using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Hub;

namespace HubUnitTests
{
    [TestClass]
    public sealed class StreamApi
    {
        [TestMethod]
        public void GetStreamInfo()
        {
            using var hub0 = new Hub("hub 0");
            hub0.PathThrough = true;
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            hub1.PathThrough = true;
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            hub0.Connect(hub1EndPoint).Wait();

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world!"));

            var streamId = hub0.RegisterStream(stream);

            var remoteStream = hub1.GetRemoteStream(hub0.Id, streamId);

            remoteStream.Wait();

            Assert.AreEqual(stream.Length, remoteStream.Result.Length);
            Assert.AreEqual(stream.Position, remoteStream.Result.Position);
            Assert.AreEqual(stream.CanRead, remoteStream.Result.CanRead);
            Assert.AreEqual(stream.CanSeek, remoteStream.Result.CanSeek);
            Assert.AreEqual(stream.CanWrite, remoteStream.Result.CanWrite);
        }

        [TestMethod]
        public void ReadRemoteStream()
        {
            using var hub0 = new Hub("hub 0");
            hub0.PathThrough = true;
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            hub1.PathThrough = true;
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            hub0.Connect(hub1EndPoint).Wait();

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world!"));

            var streamId = hub0.RegisterStream(stream);

            var remoteStreamTask = hub1.GetRemoteStream(hub0.Id, streamId);

            remoteStreamTask.Wait();
            var remoteStream = remoteStreamTask.Result;

            var data = remoteStream.Read(5);
            data.Wait();

            Assert.AreEqual("Hello", Encoding.UTF8.GetString(data.Result));
            Assert.AreEqual(5, remoteStream.Position);

            data = remoteStream.Read(8);
            data.Wait();

            Assert.AreEqual(", world!", Encoding.UTF8.GetString(data.Result));
            Assert.AreEqual(13, remoteStream.Position);
        }

        [TestMethod]
        public void WriteRemoteStream()
        {
            using var hub0 = new Hub("hub 0");
            hub0.PathThrough = true;
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            hub1.PathThrough = true;
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            hub0.Connect(hub1EndPoint).Wait();

            var stream = new MemoryStream();

            var streamId = hub0.RegisterStream(stream);

            var remoteStreamTask = hub1.GetRemoteStream(hub0.Id, streamId);

            remoteStreamTask.Wait();
            var remoteStream = remoteStreamTask.Result;

            var writeTask = remoteStream.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
            writeTask.GetAwaiter().GetResult();

            Assert.AreEqual("Hello", Encoding.UTF8.GetString(stream.GetBuffer(), 0, 5));
            Assert.AreEqual(5, remoteStream.Position);

            writeTask = remoteStream.WriteAsync(Encoding.UTF8.GetBytes(", world!"));
            writeTask.GetAwaiter().GetResult();

            Assert.AreEqual("Hello, world!", Encoding.UTF8.GetString(stream.GetBuffer(), 0, 13));
            Assert.AreEqual(13, remoteStream.Position);
        }

        [TestMethod]
        public void CloseRemoteStream()
        {
            using var hub0 = new Hub("hub 0");
            hub0.PathThrough = true;
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            hub1.PathThrough = true;
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            hub0.Connect(hub1EndPoint).Wait();

            var stream = new MemoryStream();

            var streamId = hub0.RegisterStream(stream);

            var remoteStreamTask = hub1.GetRemoteStream(hub0.Id, streamId);

            remoteStreamTask.Wait();
            var remoteStream = remoteStreamTask.Result;

            remoteStream.Close();

            Assert.ThrowsException<AggregateException>(() => hub1.GetRemoteStream(hub0.Id, streamId).Wait(), "Unknown stream");
        }

        [TestMethod]
        public void SetLengthOfRemoteStream()
        {
            using var hub0 = new Hub("hub 0");
            hub0.PathThrough = true;
            var hub0EndPoint = new IPEndPoint(IPAddress.Loopback, 4500);
            hub0.StartListening(hub0EndPoint);

            using var hub1 = new Hub("hub 1");
            hub1.PathThrough = true;
            var hub1EndPoint = new IPEndPoint(IPAddress.Loopback, 4501);
            hub1.StartListening(hub1EndPoint);

            hub0.Connect(hub1EndPoint).Wait();

            var stream = new MemoryStream();

            var streamId = hub0.RegisterStream(stream);

            var remoteStreamTask = hub1.GetRemoteStream(hub0.Id, streamId);

            remoteStreamTask.Wait();
            var remoteStream = remoteStreamTask.Result;

            remoteStream.SetLength(100);

            Assert.AreEqual(100, remoteStream.Length);
            Assert.AreEqual(100, stream.Length);
        }
    }
}
