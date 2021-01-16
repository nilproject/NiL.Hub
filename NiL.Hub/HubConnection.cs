using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NiL.Exev;

namespace NiL.Hub
{
    public sealed partial class HubConnection : IHubConnection
    {
        private const int _DisconnectTimeout = 10_000;
        private const int _HandshakeTimeout = 100_000;

        public RemoteHub RemoteHub { get; private set; }
        public IPEndPoint IPEndPoint { get; private set; }
        public HubConnectionState State { get; private set; } = HubConnectionState.NotInitialized;
        IEnumerable<RemoteHub> IHubConnection.Hubs => _allHubs.Select(x => _localHub._knownHubs[x]);

        internal readonly Hub _localHub;
        internal readonly HashSet<long> _allHubs = new HashSet<long>();

        private readonly object _sync = new object();
        private readonly Socket _socket;

        private long _writeSeqNumber;
        private long _readSeqNumber;
        private bool _reconnectOnFail;
        private Thread _thread;

        private readonly MemoryStream _outputBuffer;
        private readonly BinaryWriter _outputBufferWritter;

        private readonly MemoryStream _inputBuffer;
        private readonly BinaryReader _inputBufferReader;

        private HubConnection()
        {
            _outputBuffer = new MemoryStream();
            _outputBufferWritter = new BinaryWriter(_outputBuffer);

            _inputBuffer = new MemoryStream();
            _inputBufferReader = new BinaryReader(_inputBuffer);
        }

        private HubConnection(Hub hub, Socket socket)
            : this()
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            IPEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            _localHub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        internal bool TryGetLocked(out Locked<HubConnection> lockedHubConnection)
        {
            if (!Monitor.TryEnter(_sync))
            {
                lockedHubConnection = null;
                return false;
            }

            lockedHubConnection = new Locked<HubConnection>(this, _sync);
            return true;
        }

        internal Locked<HubConnection> GetLocked()
        {
            Monitor.Enter(_sync);
            return new Locked<HubConnection>(this, _sync);
        }

        internal static HubConnection Connect(Hub localHub, IPEndPoint endPoint)
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect(endPoint);

            if (tcpClient.Connected)
            {
                var hubConnection = AcceptConnected(localHub, tcpClient);
                hubConnection._reconnectOnFail = true;

                hubConnection.doHandshake();

                return hubConnection;
            }

            return null;
        }

        private void doHandshake()
        {
            sendHello();

            waitStateChange(HubConnectionState.Active, _HandshakeTimeout, "Timeout while handshake");
        }

        private void waitStateChange(HubConnectionState expectedState, int timeout, string errorMessage)
        {
            var startWaiting = Environment.TickCount;

            while (State != expectedState && Environment.TickCount - startWaiting < timeout)
                Thread.Sleep(1);

            if (Monitor.TryEnter(_sync, Math.Max(0, timeout - (Environment.TickCount - startWaiting))))
                Monitor.Exit(_sync);

            if (State != expectedState)
                throw new TimeoutException(errorMessage);
        }

        internal static HubConnection AcceptConnected(Hub localHub, TcpClient tcpClient)
        {
            var connectionHolder = new HubConnection(localHub, tcpClient.Client);

            lock (localHub._hubsConnctions)
            {
                if (!localHub._hubsConnctions.TryGetValue(connectionHolder.IPEndPoint, out var hubConnections))
                    localHub._hubsConnctions[connectionHolder.IPEndPoint] = hubConnections = new List<HubConnection>();

                hubConnections.Add(connectionHolder);
            }

            connectionHolder.startWorker();

            return connectionHolder;
        }

        private void startWorker()
        {
            var thread = new Thread(hubConnectionWorker)
            {
                Name = "Workder for connection from \"" + _localHub.Name + "\" (" + _localHub.Id + ") to " + IPEndPoint
            };
            _thread = thread;
            thread.Start();
        }

        private void hubConnectionWorker()
        {
            List<Action> doAfter = new List<Action>();
            for (; ; )
            {
                var bytesToRead = 0;
                while (_socket.Connected)
                {
                    _socket.Poll(10000, SelectMode.SelectRead);

                    while (_socket.Available > 2 || (bytesToRead > 0 && _socket.Available > 0))
                    {
                        try
                        {
                            var chunkSize = bytesToRead == 0 ? 2 : Math.Min(_socket.Available, bytesToRead);
                            var oldLen = _inputBuffer.Length;
                            _inputBuffer.SetLength(oldLen + chunkSize);
                            _socket.Receive(_inputBuffer.GetBuffer(), (int)oldLen, chunkSize, SocketFlags.None);

                            if (bytesToRead == 0)
                                bytesToRead = _inputBufferReader.ReadUInt16(); // size of data
                            else
                                bytesToRead -= chunkSize;

                            if (bytesToRead == 0)
                            {
                                doAfter.Clear();

                                lock (_sync)
                                {
                                    processReceived(doAfter, RemoteHub == null ? -1 : RemoteHub.Id, (int)_inputBuffer.Length);
                                    _inputBuffer.SetLength(0);

                                    if (doAfter.Count != 0)
                                    {
                                        for (var i = 0; i < doAfter.Count; i++)
                                            doAfter[i].Invoke();
                                    }

                                    if (_outputBuffer.Length != 0)
                                        FlushOutputBuffer();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);
                        }
                    }
                }

                if (_reconnectOnFail)
                {
                    Thread.Sleep(10000);
                    _socket.Connect(IPEndPoint);
                    if (_socket.Connected)
                        doHandshake();
                }
                else
                    break;
            }
        }

        internal void FlushOutputBuffer()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            if (_outputBuffer.Length > ushort.MaxValue)
            {
                _outputBuffer.SetLength(0);
                throw new InvalidOperationException("Package is to large");
            }

            var len = (ushort)_outputBuffer.Length;
            if (len == 0)
                return;

            try
            {
                if (_socket.Poll(1000, SelectMode.SelectWrite))
                {
                    _outputBufferWritter.Write(len); // size

                    var buffer = _outputBuffer.GetBuffer();

                    var segments = new ArraySegment<byte>[2];
                    segments[0] = new ArraySegment<byte>(buffer, len, 2);
                    segments[1] = new ArraySegment<byte>(buffer, 0, len);

                    try
                    {
                        _socket.Send(segments);
                    }
                    catch (SocketException)
                    {
                        if (_socket.Connected)
                            throw;
                    }
                }
            }
            finally
            {
                _outputBuffer.SetLength(0);
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                _reconnectOnFail = false;
                State = HubConnectionState.Disconnecting;
                writeDisconnect();
                FlushOutputBuffer();
            }

            invalidateConnection();
            waitStateChange(HubConnectionState.Disconnected, _DisconnectTimeout, "Timeout while disconnecting");
        }

        private void sendHello()
        {
            lock (_sync)
            {
                if (State != HubConnectionState.NotInitialized)
                    throw new InvalidOperationException();

                writeHello();
                FlushOutputBuffer();

                State = HubConnectionState.HelloSent;
            }
        }
    }
}
