using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NiL.Hub
{
    internal class SharedInterface<TInterface> : SharedInterface, ISharedInterface<TInterface> where TInterface : class
    {
        private readonly Hub _localHub;

        public SharedInterface(Hub hub, string fullName)
            : this(hub, fullName, new List<RemoteHubInterfaceLink>())
        {

        }

        public SharedInterface(Hub hub, string fullName, List<RemoteHubInterfaceLink> remoteHubs)
            : base(fullName, remoteHubs)
        {
            _localHub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        public Task<TResult> Call<TResult>(Expression<Func<TInterface, TResult>> expression, int shareId = default) => callImpl<TResult>(expression, shareId);

        public Task<TResult> Call<TResult>(Expression<Func<TInterface, Task<TResult>>> expression, int shareId = default) => callImpl<TResult>(expression, shareId);

        private Task<TResult> callImpl<TResult>(Expression expression, int shareId)
        {
            if (LocalImplementation != null && (shareId == 0 || LocalShareId == shareId))
                return Task.FromResult((TResult)_localHub._expressionEvaluator.Eval(expression));

            var taskSource = sendExpression(expression, shareId);

            return taskSource.Task.ContinueWith(x =>
            {
                var task = taskSource.Task;
                if (task.IsFaulted)
                    throw task.Exception.InnerException;

                return (TResult)task.Result;
            });
        }

        private TaskCompletionSource<object> sendExpression(Expression expression, int shareId)
        {
            while (HubLinks.Count > 0)
            {
                var index = Environment.TickCount % HubLinks.Count;
                var hubLink = HubLinks[index];

                if (shareId != 0 && hubLink.ShareId != shareId)
                {
                    for (var i = 0; i < HubLinks.Count; i++)
                    {
                        var h = HubLinks[(index + i) % HubLinks.Count];
                        if (h.ShareId == shareId)
                        {
                            hubLink = h;
                            break;
                        }
                    }

                    if (hubLink.ShareId != shareId)
                        throw new ShareNotFoundException(_localHub, typeof(TInterface), shareId);
                }

                hubLink.Hub._typesMap.TryAdd(typeof(TInterface), hubLink.InterfaceId);

                var taskSource = _localHub.AllocTaskCompletionSource(out var taskAwaitId);

                var serializedExpression = hubLink.Hub._expressionSerializer.Serialize(expression, Array.Empty<ParameterExpression>());

                try
                {
                    using var connection = hubLink.Hub._connections.GetLockedConenction();
                    if (connection == null)
                        continue;

                    if (connection.Value.RemoteHub.Id != hubLink.Hub.Id)
                        connection.Value.WriteRetransmitTo(hubLink.Hub.Id, c => c.WriteCall(taskAwaitId, serializedExpression));
                    else
                        connection.Value.WriteCall(taskAwaitId, serializedExpression);
                    connection.Value.FlushOutputBuffer();

                    return taskSource;
                }
                catch (HubDisconnectedException e)
                {
                    Console.Error.WriteLine(e);
                }
            }

            throw new InvalidOperationException("All hubs are disconnected");
        }

        public Task Call(Expression<Action<TInterface>> expression, int version = default)
        {
            if (LocalImplementation != null && (version == 0 || LocalShareId == version))
                return Task.FromResult(_localHub._expressionEvaluator.Eval(expression));

            var taskSource = sendExpression(expression, version);
            var result = taskSource.Task;
            result.ContinueWith(x =>
            {
                GC.KeepAlive(taskSource);
            });
            return result;
        }
    }
}
