using System;
using System.Collections.Generic;
using System.Net;

namespace NiL.Hub
{
    public interface IHubConnection
    {
        RemoteHub RemoteHub { get; }
        IEnumerable<RemoteHub> Hubs { get; }
        EndPoint EndPoint { get; }
        HubConnectionState State { get; }

        void Disconnect();

        event EventHandler<ConnectionEventArgs> Disconnected;
        event EventHandler<ConnectionEventArgs> Connected;
    }
}
