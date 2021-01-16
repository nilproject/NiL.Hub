using System;
using System.Threading;

namespace NiL.Hub
{
    internal sealed class Locked<T> : IDisposable
    {
        public readonly T Value;
        private readonly object _sync;

        public Locked(T value, object sync)
        {
            Value = value;
            _sync = sync;
        }

        public void Dispose()
        {
            Monitor.Exit(_sync);
        }
    }
}
