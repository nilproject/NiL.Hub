using System;

namespace NiL.Hub
{
    public sealed class HubDisconnectedException : Exception
    {
        public HubDisconnectedException(RemoteHub remoteHub, Hub localHub, string message = null)
            : base(message ?? "Remote hub has disconnected")
        {
            RemoteHub = remoteHub;
            LocalHub = localHub;
        }

        public RemoteHub RemoteHub { get; }
        public Hub LocalHub { get; }
    }
}
