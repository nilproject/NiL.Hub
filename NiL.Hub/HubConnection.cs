using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public sealed partial class HubConnection : IHubConnection, IDisposable
    {
        private const int _DisconnectTimeout = 10_000;
        private const int _HandshakeTimeout = 10_000;

        public RemoteHub RemoteHub { get; private set; }
        public IPEndPoint IPEndPoint { get; private set; }
        public HubConnectionState State { get; private set; } = HubConnectionState.NotInitialized;
        IEnumerable<RemoteHub> IHubConnection.Hubs => _allHubs.Select(x => _localHub._knownHubs[x]);

        internal readonly Hub _localHub;
        internal readonly HashSet<long> _allHubs = new HashSet<long>();

        private readonly object _sync = new object();

        private Socket _socket;
        private long _writeSeqNumber;
        private long _readSeqNumber;
        private bool _reconnectOnFail;
        private Thread _thread;
        private long _lastActivityTimestamp;
        private readonly MemoryStream _outputBuffer;
        private readonly BinaryWriter _outputBufferWritter;

        private readonly MemoryStream _inputBuffer;
        private readonly BinaryReader _inputBufferReader;

        public event EventHandler<ConnectionEventArgs> Disconnected;
        public event EventHandler<ConnectionEventArgs> Connected;

        private HubConnection()
        {
            _outputBuffer = new MemoryStream();
            _outputBufferWritter = new BinaryWriter(_outputBuffer);

            _inputBuffer = new MemoryStream();
            _inputBufferReader = new BinaryReader(_inputBuffer);
        }

        private HubConnection(Hub hub, Socket socket, bool reconnectOnFail)
            : this()
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            IPEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            _localHub = hub ?? throw new ArgumentNullException(nameof(hub));
            _reconnectOnFail = reconnectOnFail;
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

        internal static HubConnection Connect(Hub localHub, IPEndPoint endPoint, bool autoReconnect)
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect(endPoint);

            if (tcpClient.Connected)
            {
                var hubConnection = AcceptConnected(localHub, tcpClient, autoReconnect);

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

        internal static HubConnection AcceptConnected(Hub localHub, TcpClient tcpClient, bool reconnectOnFail)
        {
            var connectionHolder = new HubConnection(localHub, tcpClient.Client, reconnectOnFail);

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
                if (_socket.Connected)
                {
                    try
                    {
                        var bytesToRead = 0;
                        while (_socket.Connected)
                        {
                            if ((DateTime.Now.Ticks - _lastActivityTimestamp) >= TimeSpan.FromSeconds(15).Ticks)
                            {
                                lock (_sync)
                                {
                                    writePing();
                                    FlushOutputBuffer();
                                }
                            }

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
                                        _lastActivityTimestamp = DateTime.Now.Ticks;
                                        doAfter.Clear();

                                        lock (_sync)
                                        {
                                            processReceived(doAfter, RemoteHub == null ? -1 : RemoteHub.Id, (int)_inputBuffer.Length - sizeof(ushort));
                                            _inputBuffer.SetLength(0);

                                            if (_outputBuffer.Length != 0)
                                                FlushOutputBuffer();
                                        }

                                        if (doAfter.Count != 0)
                                        {
                                            for (var i = 0; i < doAfter.Count; i++)
                                                doAfter[i].Invoke();
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.Error.WriteLine(e);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Console.Error.WriteLine("HubConnectionWorker stoped. " + (_reconnectOnFail ? "Try to reconnect. " : "Reconnect disabled. ") + "(Remote endpoint: " + IPEndPoint + ")");

                        State = HubConnectionState.Disconnected;

                        onDisconnected();
                    }
                }

                if (_reconnectOnFail)
                {
                    Thread.Sleep(10000);
                    try
                    {
                        _socket.Connect(IPEndPoint);
                        Console.WriteLine("Reconnected to " + IPEndPoint);
                        if (_socket.Connected)
                            sendHello();
                    }
                    catch (SocketException)
                    {
                        Console.Error.WriteLine("Unable to connect to " + IPEndPoint);
                    }
                }
                else
                    break;
            }
        }

        internal void FlushOutputBuffer()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            lock (_sync)
            {
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
                            _lastActivityTimestamp = DateTime.Now.Ticks;
                            _socket.Send(segments);
                        }
                        catch (SocketException)
                        {
                            onDisconnected();
                            throw new HubDisconnectedException(RemoteHub, _localHub);
                        }
                    }
                }
                finally
                {
                    _outputBuffer.SetLength(0);
                }
            }
        }

        public void Disconnect()
        {
            _reconnectOnFail = false;

            if (State != HubConnectionState.Active)
                return;

            try
            {
                lock (_sync)
                {
                    State = HubConnectionState.Disconnecting;
                    writeDisconnect();
                    FlushOutputBuffer();
                }
            }
            finally
            {
                onDisconnected();
            }

            waitStateChange(HubConnectionState.Disconnected, _DisconnectTimeout, "Timeout while disconnecting");
        }

        private void sendHello()
        {
            lock (_sync)
            {
                if (State != HubConnectionState.NotInitialized && State != HubConnectionState.Disconnected)
                    throw new InvalidOperationException("State must be " + HubConnectionState.NotInitialized + " or " + HubConnectionState.Disconnected);

                writeHello();
                FlushOutputBuffer();
                State = HubConnectionState.HelloSent;
            }
        }

        private void onDisconnected()
        {
            State = HubConnectionState.Disconnected;

            invalidateConnection();

            try
            {
                if (_socket.Connected)
                    _socket.Disconnect(false);
            }
            catch
            { }

            _socket = new TcpClient().Client;

            var handler = Disconnected;
            if (handler != null)
            {
                Task.Run(() => handler(this, new ConnectionEventArgs(this)));
            }
        }

        private void onConnected()
        {
            var handler = Connected;
            if (handler != null)
            {
                Task.Run(() => handler(this, new ConnectionEventArgs(this)));
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
