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
        public EndPoint EndPoint { get; private set; }
        public HubConnectionState State { get; private set; } = HubConnectionState.NotInitialized;
        IEnumerable<RemoteHub> IHubConnection.Hubs => _allHubs.Select(x => _localHub._knownHubs[x]);

        public Socket Socket => _socket;

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (!_socket.Connected
                        && State is not HubConnectionState.Disconnecting
                                 and not HubConnectionState.Disconnected
                                 and not HubConnectionState.Disposed)
                    {
                        State = HubConnectionState.Disconnected;

                        onDisconnected();

                        return false;
                    }
                }
                catch
                {
                    _socket.Dispose();
                    _socket = new TcpClient().Client;
                    State = HubConnectionState.Disconnected;
                }

                return _socket.Connected;
            }
        }

        public MemoryStream InputBuffer => _inputBuffer;

        public BinaryReader InputBufferReader => _inputBufferReader;

        public MemoryStream OutputBuffer => _outputBuffer;

        public BinaryWriter OutputBufferWritter => _outputBufferWritter;

        public Hub LocalHub => _localHub;

        public long LastActivityTimestamp => _lastActivityTimestamp;

        internal readonly HashSet<long> _allHubs = new HashSet<long>();

        private readonly object _sync = new object();

        private readonly Hub _localHub;
        private Socket _socket;
        private long _writeSeqNumber;
        private long _readSeqNumber;
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
            _inputBuffer = new MemoryStream();
            _inputBufferReader = new BinaryReader(_inputBuffer);
            _outputBufferWritter = new BinaryWriter(_outputBuffer);
        }

        public HubConnection(Hub hub, Socket socket)
            : this()
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            EndPoint = socket.RemoteEndPoint;
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

        internal static HubConnection Connect(Hub localHub, EndPoint endPoint)
        {
            if (endPoint is not IPEndPoint ipEndPoint)
                throw new NotImplementedException("Connecting to " + endPoint.GetType() + " is not implemented");

            var tcpClient = new TcpClient();
            tcpClient.Connect(ipEndPoint);

            if (tcpClient.Connected)
            {
                var connection = new HubConnection(localHub, tcpClient.Client);
                return connection;
            }

            return null;
        }

        internal void StartHandshake()
        {
            sendHello();
            waitStateChange(HubConnectionState.Active, _HandshakeTimeout, "Timeout while handshake");
        }

        private void waitStateChange(HubConnectionState expectedState, int timeout, string errorMessage)
        {
#if DEBUG
            if (Debugger.IsAttached)
                timeout = int.MaxValue;
#endif
            var startWaiting = Environment.TickCount;

            while (State != expectedState && Environment.TickCount - startWaiting < timeout)
                Thread.Sleep(1);

            if (Monitor.TryEnter(_sync, Math.Max(0, timeout - (Environment.TickCount - startWaiting))))
                Monitor.Exit(_sync);

            if (State != expectedState)
                throw new TimeoutException(errorMessage);
        }

        internal void FlushOutputBuffer()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            lock (_sync)
            {
                if (_outputBuffer.Length > ushort.MaxValue)
                {
                    _outputBuffer.SetLength(0);
                    throw new InvalidOperationException("Package is too large");
                }

                var len = (ushort)_outputBuffer.Length;
                if (len == 0)
                    return;

                try
                {
                    if (_socket.Poll(1000, SelectMode.SelectWrite))
                    {
                        //_outputBufferWritter.Write(len); // size
                        _outputBuffer.WriteByte((byte)len);
                        _outputBuffer.WriteByte((byte)(len >> 8));

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
            if (State == HubConnectionState.Disposed)
                throw new ObjectDisposedException(nameof(HubConnection));

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
            //State = HubConnectionState.Disposed;
        }

        public void Reconnect()
        {
            if (State == HubConnectionState.Disposed)
                throw new ObjectDisposedException(nameof(HubConnection));

            if (_socket.Connected)
            {
                _socket.Disconnect(false);
                _socket = new TcpClient().Client;
            }

            _socket.Connect(EndPoint);
            if (_socket.Connected)
                sendHello();
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
            if (State == HubConnectionState.Disposed)
                return;

            Disconnect();
            State = HubConnectionState.Disposed;
        }
    }
}
