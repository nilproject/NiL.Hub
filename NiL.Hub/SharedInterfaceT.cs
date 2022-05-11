using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NiL.Hub
{
    internal class SharedInterface<IInterface> : SharedInterface, ISharedInterface<IInterface> where IInterface : class
    {
        private readonly Hub _hub;

        public SharedInterface(Hub hub, string fullName)
            : this(hub, fullName, new List<RemoteHubInterfaceLink>())
        {

        }

        public SharedInterface(Hub hub, string fullName, List<RemoteHubInterfaceLink> remoteHubs)
            : base(fullName, remoteHubs)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        public Task<TResult> Call<TResult>(Expression<Func<IInterface, TResult>> expression, int version = default) => callImpl<TResult>(expression, version);

        public Task<TResult> Call<TResult>(Expression<Func<IInterface, Task<TResult>>> expression, int version = default) => callImpl<TResult>(expression, version);

        private Task<TResult> callImpl<TResult>(Expression expression, int version)
        {
            if (LocalImplementation != null && (version == 0 || LocalVersion == version))
                return Task.FromResult((TResult)_hub._expressionEvaluator.Eval(expression));

            var taskSource = sendExpression(expression, version);

            return taskSource.Task.ContinueWith(x =>
            {
                var task = taskSource.Task;
                if (task.IsFaulted)
                    throw task.Exception.InnerException;

                return (TResult)task.Result;
            });
        }

        private TaskCompletionSource<object> sendExpression(Expression expression, int version)
        {
            while (Hubs.Count > 0)
            {
                var index = Environment.TickCount % Hubs.Count;
                var hub = Hubs[index];

                if (version != 0 && hub.Version != version)
                {
                    for (var i = 0; i < Hubs.Count; i++)
                    {
                        var h = Hubs[(index + i) % Hubs.Count];
                        if (h.Version == version)
                        {
                            hub = h;
                            break;
                        }
                    }
                }

                hub.Hub._typesMap.TryAdd(typeof(IInterface), hub.InterfaceId);

                var taskSource = _hub.AllocTaskCompletionSource(out var taskAwaitId);

                var serializedExpression = hub.Hub._expressionSerializer.Serialize(expression, Array.Empty<ParameterExpression>());

                try
                {
                    using var connection = hub.Hub._connections.GetLockedConenction();
                    if (connection == null)
                        continue;

                    connection.Value.WriteRetransmitTo(hub.Hub.Id, c => c.WriteCall(taskAwaitId, serializedExpression));
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

        public Task Call(Expression<Action<IInterface>> expression, int version = default)
        {
            if (LocalImplementation != null && (version == 0 || LocalVersion == version))
                return Task.FromResult(_hub._expressionEvaluator.Eval(expression));

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
