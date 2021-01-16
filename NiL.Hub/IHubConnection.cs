using System.Collections.Generic;
using System.Net;

namespace NiL.Hub
{
    public interface IHubConnection
    {
        RemoteHub RemoteHub { get; }
        IEnumerable<RemoteHub> Hubs { get; }
        IPEndPoint IPEndPoint { get; }
        HubConnectionState State { get; }

        void Disconnect();
    }
}
