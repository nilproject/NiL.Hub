using System;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public class RemoteStream : IDisposable
    {
        private readonly object _sync = new object();
        private readonly Hub _localHub;
        private readonly RemoteHub _remoteHub;
        private readonly int _streamId;
        private long _length;
        private long _position;
        private bool _canRead;
        private bool _canWrite;
        private bool _canSeek;
        private TaskCompletionSource<byte[]> _awaiter;

        public RemoteStream(
            Hub localHub,
            RemoteHub remoteHub,
            int streamId,
            long length,
            long position,
            bool canRead,
            bool canWrite,
            bool canSeek)
        {
            _localHub = localHub;
            _remoteHub = remoteHub ?? throw new ArgumentNullException(nameof(remoteHub));
            _streamId = streamId;
            _length = length;
            _position = position;
            _canRead = canRead;
            _canWrite = canWrite;
            _canSeek = canSeek;
        }

        public long Length { get => _length; internal set => _length = value; }
        public long Position { get => _position; internal set => _position = value; }
        public bool CanRead { get => _canRead; internal set => _canRead = value; }
        public bool CanWrite { get => _canWrite; internal set => _canWrite = value; }
        public bool CanSeek { get => _canSeek; internal set => _canSeek = value; }
        public bool Closed { get; internal set; }

        ~RemoteStream()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Close();
        }

        public void Close()
        {
            if (Closed)
                return;
            Closed = true;

            _localHub._remoteStreams.Remove((_remoteHub.Id, _streamId));

            using var connection = _remoteHub._connections.GetLockedConenction();
            connection.Value.WriteStreamClose(_streamId);
            connection.Value.FlushOutputBuffer();
        }

        public Task<byte[]> Read(ushort length)
        {
            lock (_sync)
            {
                if (_awaiter != null)
                    throw new InvalidOperationException("Stream already in awaiting state");

                using var connection = _remoteHub._connections.GetLockedConenction();
                connection.Value.WriteRetransmitTo(_remoteHub.Id, x =>
                {
                    x.WriteStreamRead(_streamId, length);
                });

                connection.Value.FlushOutputBuffer();

                _awaiter = new TaskCompletionSource<byte[]>();
                return _awaiter.Task;
            }
        }

        public Task Write(ArraySegment<byte> data)
        {
            lock (_sync)
            {
                if (_awaiter != null)
                    throw new InvalidOperationException("Stream already in awaiting state");

                using var connection = _remoteHub._connections.GetLockedConenction();
                connection.Value.WriteRetransmitTo(_remoteHub.Id, x =>
                {
                    x.WriteStreamWrite(_streamId, data);
                });

                connection.Value.FlushOutputBuffer();

                _awaiter = new TaskCompletionSource<byte[]>();
                return _awaiter.Task;
            }
        }

        internal void ReceiveData(byte[] data)
        {
            try
            {
                _awaiter?.SetResult(data);
            }
            finally
            {
                _awaiter = null;
            }
        }
    }
}