using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public interface IHub : IDisposable
    {
        string Name { get; }
        long Id { get; }

        ISharedInterface<TInterface> Get<TInterface>() where TInterface : class;

        void StartListening(IPEndPoint endPoint);
        void StopListening(IPEndPoint endPoint);

        Task Connect(IPEndPoint endPoint);

        Task RegisterInterface<TInterface>(TInterface implementation) where TInterface : class;
        Task UnRegisterInterface<TInterface>() where TInterface : class;

        IEnumerable<RemoteHub> KnownHubs { get; }
        IEnumerable<HubConnection> Connections { get; }
    }
}
