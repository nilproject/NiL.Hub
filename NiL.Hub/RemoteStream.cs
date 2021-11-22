using System;
using System.IO;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public class RemoteStream : Stream, IDisposable
    {
        private readonly object _sync = new();
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

        public override long Length { get => _length; }
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }
        public override bool CanRead { get => _canRead; }
        public override bool CanWrite { get => _canWrite; }
        public override bool CanSeek { get => _canSeek; }
        public bool Closed { get; internal set; }

        ~RemoteStream()
        {
            Dispose();
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);

            Close();
        }

        public override void Close()
        {
            if (Closed)
                return;

            Closed = true;

            _localHub._remoteStreams.Remove((_remoteHub.Id, _streamId));

            using var connection = _remoteHub._connections.GetLockedConenction();
            connection.Value.WriteStreamClose(_streamId);
            connection.Value.FlushOutputBuffer();

            base.Close();
        }

        public Task<byte[]> Read(ushort length)
        {
            if (Closed)
                throw new InvalidOperationException("Stream closed");

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
            if (Closed)
                throw new InvalidOperationException("Stream closed");

            if (!_canWrite)
                throw new InvalidOperationException();

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

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Closed)
                throw new InvalidOperationException("Stream closed");

            if (!_canRead)
                throw new InvalidOperationException();

            if (count < 0 || count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset) + " + " + nameof(count));

            var bytes = Read(checked((ushort)count))
                .GetAwaiter()
                .GetResult();

            Array.Copy(bytes, 0, buffer, offset, count);
            return bytes.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (Closed)
                throw new InvalidOperationException("Stream closed");

            if (!_canSeek)
                throw new InvalidOperationException();

            lock (_sync)
            {
                if (_awaiter != null)
                    throw new InvalidOperationException("Stream already in awaiting state");

                using var connection = _remoteHub._connections.GetLockedConenction();
                connection.Value.WriteRetransmitTo(_remoteHub.Id, x =>
                {
                    x.WriteStreamSeek(_streamId, offset, origin);
                });

                connection.Value.FlushOutputBuffer();

                _awaiter = new TaskCompletionSource<byte[]>();
            }

            _awaiter.Task.GetAwaiter().GetResult();

            return _position;
        }

        public override void SetLength(long value)
        {
            if (Closed)
                throw new InvalidOperationException("Stream closed");

            if (_awaiter != null)
                throw new InvalidOperationException("Stream already in awaiting state");

            lock (_sync)
            {
                using var connection = _remoteHub._connections.GetLockedConenction();
                connection.Value.WriteRetransmitTo(_remoteHub.Id, x =>
                {
                    x.WriteStreamSetLength(_streamId, value);
                });

                connection.Value.FlushOutputBuffer();

                _awaiter = new TaskCompletionSource<byte[]>();
            }

            _awaiter.Task.GetAwaiter().GetResult();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(new ArraySegment<byte>(buffer, offset, count)).Wait();
        }

        internal void SetCanRead(bool v)
        {
            _canRead = v;
        }

        internal void SetCanWrite(bool v)
        {
            _canWrite = v;
        }

        internal void SetCanSeek(bool v)
        {
            _canSeek = v;
        }

        internal void SetPosition(long position)
        {
            _position = position;
        }

        internal void SetLengthInternal(long length)
        {
            _length = length;
        }
    }
}