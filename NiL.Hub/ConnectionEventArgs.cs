using System;
using System.Net;

namespace NiL.Hub
{
    public sealed class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(IHubConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public IHubConnection Connection { get; }
    }
}
