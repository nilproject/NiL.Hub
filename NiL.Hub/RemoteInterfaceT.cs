using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NiL.Hub
{
    internal class SharedInterface<IInterface> : SharedInterface, ISharedInterface<IInterface> where IInterface : class
    {
        private readonly Hub _localHub;

        public SharedInterface(Hub localHub, string fullName)
            : this(localHub, fullName, new List<RemoteHubInterfaceLink>())
        {

        }

        public SharedInterface(Hub localHub, string fullName, List<RemoteHubInterfaceLink> remoteHubs)
            : base(fullName, remoteHubs)
        {
            _localHub = localHub ?? throw new ArgumentNullException(nameof(localHub));
        }

        public Task<TResult> Call<TResult>(Expression<Func<IInterface, TResult>> expression)
        {
            var hub = Hubs[Environment.TickCount % Hubs.Count];

            if (!hub.Hub._typesMap.HasOwn(typeof(IInterface)))
                hub.Hub._typesMap.Add(typeof(IInterface), hub.InterfaceId);

            var taskSource = _localHub.AllocTaskCompletionSource(out var taskAwaitId);

            var serializedExpression = hub.Hub._expressionSerializer.Serialize(expression, Array.Empty<ParameterExpression>());

            using var connection = hub.Hub._connections.GetLockedConenction();
            connection.Value.WriteRetransmitTo(hub.Hub.Id, c => c.WriteCall(taskAwaitId, serializedExpression));
            connection.Value.FlushOutputBuffer();

            return taskSource.Task.ContinueWith(x =>
            {
                GC.KeepAlive(taskSource); // should live while task is alive

                return (TResult)x.Result;
            });
        }
    }
}
