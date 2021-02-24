using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NiL.Exev;

namespace NiL.Hub
{
    public partial class Hub : IHub
    {
        private uint _interfaceIdCounter;
        private volatile int _callResultAwaitId;

        internal readonly TypesMapLayer _commonTypesMap = new TypesMapLayer();
        internal readonly Dictionary<string, SharedInterface> _knownInterfaces = new Dictionary<string, SharedInterface>();
        internal readonly Dictionary<IPEndPoint, Thread> _listeners = new Dictionary<IPEndPoint, Thread>();
        internal readonly Dictionary<IPEndPoint, List<HubConnection>> _hubsConnctions = new Dictionary<IPEndPoint, List<HubConnection>>();
        internal readonly Dictionary<long, RemoteHub> _knownHubs = new Dictionary<long, RemoteHub>();
        internal readonly ExpressionEvaluator _expressionEvaluator = new ExpressionEvaluator();
        internal readonly ExpressionDeserializer _expressionDeserializer;
        internal readonly Dictionary<int, WeakReference<TaskCompletionSource<object>>> _awaiters = new Dictionary<int, WeakReference<TaskCompletionSource<object>>>();

        public IEnumerable<RemoteHub> KnownHubs => _knownHubs.Values;
        public IEnumerable<HubConnection> Connections => _hubsConnctions.Values.SelectMany(x => x);
        public IEnumerable<IPEndPoint> EndPoints => _listeners.Count > 0 ? _listeners.Select(x => x.Key) : default;

        public bool PathThrough { get; set; }

        public Hub()
            : this("<unnamed hub>")
        { }

        public Hub(string name)
            : this((uint)name.GetHashCode(), name)
        { }

        public Hub(long id, string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = id;

            _commonTypesMap = new TypesMapLayer();
            _expressionDeserializer = new ExpressionDeserializer(_commonTypesMap);
        }

        public string Name { get; private set; }

        public long Id { get; private set; }

        public ISharedInterface<TInterface> Get<TInterface>() where TInterface : class
        {
            if (!TryGet<TInterface>(out var result))
                throw new InvalidOperationException("Unknown interface \"" + typeof(TInterface).FullName + "\"");

            return result;
        }

        public bool TryGet<TInterface>(out ISharedInterface<TInterface> remoteInterface) where TInterface : class
        {
            var interfaceFullName = typeof(TInterface).FullName;

            lock (_knownInterfaces)
            {
                if (!_knownInterfaces.TryGetValue(interfaceFullName, out var remInterface))
                {
                    remoteInterface = null;
                    return false;
                }

                if (!(remInterface is SharedInterface<TInterface>))
                {
                    remInterface = new SharedInterface<TInterface>(this, remInterface.Name, remInterface.Hubs);
                    _knownInterfaces[interfaceFullName] = remInterface;
                }

                remoteInterface = remInterface as ISharedInterface<TInterface>;
                return true;
            }
        }

        internal object GetLocalImplementation(Type type)
        {
            lock (_knownInterfaces)
            {
                if (!_knownInterfaces.TryGetValue(type.FullName, out var remInterface))
                    throw new ArgumentException(type.FullName + " is unknown");

                if (remInterface.LocalImplementation == null)
                    throw new InvalidOperationException(type.FullName + " has no local implementation");

                return remInterface.LocalImplementation;
            }
        }

        public void Dispose()
        {
            var endPoints = _listeners.Keys.ToArray();
            for (var i = 0; i < endPoints.Length; i++)
                StopListening(endPoints[i]);

            var connectionsGroups = _hubsConnctions.ToArray();
            foreach (var connections in connectionsGroups)
            {
                foreach (var connection in connections.Value.ToArray())
                    connection.Disconnect();
            }

            GC.SuppressFinalize(this);
        }

        public void StartListening(IPEndPoint endPoint)
        {
            if (_listeners.ContainsKey(endPoint))
                return;

            var listener = new TcpListener(endPoint);
            listener.Start();
            endPoint = (IPEndPoint)listener.LocalEndpoint;
            try
            {
                var thread = new Thread(x => listenerWorker(x as TcpListener));
                thread.Name = "Listener of " + Name + " (" + Id + ")";
                _listeners[endPoint] = thread;
                thread.Start(listener);
            }
            catch
            {
                listener.Stop();
            }
        }

        public void StopListening(IPEndPoint endPoint)
        {
            if (!_listeners.ContainsKey(endPoint))
                return;

            var thread = _listeners[endPoint];
            _listeners.Remove(endPoint);

            var startWaiting = Environment.TickCount;
            while (thread.IsAlive && Environment.TickCount - startWaiting < 5_000)
                Thread.Sleep(1);

            if (thread.IsAlive)
                throw new TimeoutException("Timeout while stopping listening");
        }

        private void listenerWorker(TcpListener listener)
        {
            try
            {
                var endPoint = listener.LocalEndpoint as IPEndPoint;
                var thread = Thread.CurrentThread;
                while (_listeners.ContainsKey(endPoint) && _listeners[endPoint] == thread)
                {
                    listener.Server.Poll(10000, SelectMode.SelectRead);

                    if (listener.Pending())
                    {
                        var tcpClient = listener.AcceptTcpClient();
                        if (tcpClient != null)
                        {
                            HubConnection.AcceptConnected(this, tcpClient);
                        }
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        public Task Connect(IPEndPoint endPoint)
        {
            return Task.Run(() =>
            {
                var hubConnection = HubConnection.Connect(this, endPoint);
            });
        }

        internal RemoteHub HubIsAvailableThrough(HubConnection hubConnection, long hubId, int distance, string hubName)
        {
            lock (_knownHubs)
            {
                if (_knownHubs.TryGetValue(hubId, out var hub))
                {
                    lock (hub)
                    {
                        hub._connections.Set(hubConnection, distance);
                    }
                }
                else
                {
                    hub = new RemoteHub(new TypesMapLayer(_commonTypesMap))
                    {
                        Id = hubId,
                        Name = hubName
                    };

                    hub._connections.Set(hubConnection, distance);

                    _knownHubs.Add(hubId, hub);
                }

                return hub;
            }
        }

        private RemoteHub getRemoteHub(long hubId)
        {
            lock (_knownHubs)
            {
                if (!_knownHubs.TryGetValue(hubId, out var hub))
                    throw new ArgumentException("Unknown hub #" + hubId);

                return hub;
            }
        }

        internal void HubIsUnavailableThrough(HubConnection hubConnection, long hubId)
        {
            lock (_knownHubs)
            {
                if (_knownHubs.TryGetValue(hubId, out var hub))
                {
                    lock (hub)
                    {
                        hub._connections.Remove(hubConnection);

                        if (hub._connections.Count == 0)
                        {
                            _knownHubs.Remove(hub.Id);

                            var interfacesToRemove = default(List<string>);
                            lock (_knownInterfaces)
                            {
                                foreach (var interfaceName in hub._interfaces)
                                {
                                    var @interface = _knownInterfaces[interfaceName];
                                    @interface.Hubs.RemoveAll(x => x.Hub.Id == hub.Id);

                                    if (@interface.Hubs.Count == 0)
                                    {
                                        if (interfacesToRemove == null)
                                            interfacesToRemove = new List<string>();

                                        interfacesToRemove.Add(interfaceName);
                                    }
                                }

                                if (interfacesToRemove != null)
                                {
                                    foreach (var interfaceName in interfacesToRemove)
                                    {
                                        _knownInterfaces.Remove(interfaceName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public Task RegisterInterface<TInterface>(TInterface implementation) where TInterface : class
        {
            return Task.Run(() =>
            {
                var interfaceName = typeof(TInterface).FullName;
                var interfaceId = ++_interfaceIdCounter;

                lock (_knownInterfaces)
                {
                    if (_knownInterfaces.TryGetValue(interfaceName, out var knownInterface))
                    {
                        if (knownInterface.LocalImplementation != null)
                            throw new InvalidOperationException("\"" + interfaceName + "\" already localy registered");
                    }
                    else
                    {
                        _knownInterfaces[interfaceName] = knownInterface = new SharedInterface<TInterface>(this, interfaceName);
                    }

                    knownInterface.LocalImplementation = implementation;
                    _commonTypesMap.Add(typeof(TInterface), interfaceId);
                }

                foreach (var connections in _hubsConnctions)
                {
                    using var connection = connections.Value[0].GetLocked();
                    connection.Value.WriteRegisterInterface(Id, interfaceName, interfaceId);
                    connection.Value.FlushOutputBuffer();
                }
            });
        }

        public Task UnRegisterInterface<TInterface>() where TInterface : class
        {
            throw new NotImplementedException();
        }

        internal TaskCompletionSource<object> AllocTaskCompletionSource(out int taskAwaitId)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            lock (_awaiters)
            {
                taskAwaitId = Interlocked.Increment(ref _callResultAwaitId);
                _awaiters.Add(taskAwaitId, new WeakReference<TaskCompletionSource<object>>(taskCompletionSource));
            }

            return taskCompletionSource;
        }

        internal Task Eval(long hubId, int awaitId, byte[] code)
        {
            return Task.Run(() =>
            {
                var hub = getRemoteHub(hubId);
                using var connection = hub._connections.GetLockedConenction();

                try
                {
                    var deserialized = (LambdaExpression)_expressionDeserializer.Deserialize(code, Array.Empty<ParameterExpression>());
                    if (deserialized.Parameters.Count != 1)
                        throw new ArgumentException("Number of parameters is not equal to 1");

                    object implementation = GetLocalImplementation(deserialized.Parameters[0].Type);
                    var result = _expressionEvaluator.Eval(deserialized.Body, new Parameter(deserialized.Parameters[0], implementation));

                    connection.Value.WriteRetransmitTo(hubId, c => c.WriteResult(awaitId, hub._expressionSerializer.Serialize(Expression.Constant(result))));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    connection.Value.WriteRetransmitTo(hubId, c => c.WriteException(awaitId, e));
                }

                connection.Value.FlushOutputBuffer();
            });
        }

        internal Task SetResult(int awaitId, byte[] code)
        {
            return Task.Run(() =>
            {
                try
                {
                    WeakReference<TaskCompletionSource<object>> awaiterRef;
                    var knownAwaiter = false;
                    lock (_awaiters)
                        knownAwaiter = _awaiters.TryGetValue(awaitId, out awaiterRef);

                    if (!knownAwaiter)
                    {
                        Console.Error.WriteLine("Unknown awaiter #" + awaitId);
                        return;
                    }

                    if (!awaiterRef.TryGetTarget(out var awaiter))
                        return;

                    var deserialized = _expressionDeserializer.Deserialize(code, Array.Empty<ParameterExpression>());
                    var value = _expressionEvaluator.Eval(deserialized, Array.Empty<Parameter>());
                    awaiter.SetResult(value);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });
        }

        internal Task SetException(int awaitId, string message)
        {
            return Task.Run(() =>
            {
                try
                {
                    WeakReference<TaskCompletionSource<object>> awaiterRef;
                    var knownAwaiter = false;
                    lock (_awaiters)
                        knownAwaiter = _awaiters.TryGetValue(awaitId, out awaiterRef);

                    if (!knownAwaiter)
                    {
                        Console.Error.WriteLine("Unknown awaiter #" + awaitId);
                        return;
                    }

                    if (!awaiterRef.TryGetTarget(out var awaiter))
                        return;

                    awaiter.SetException(new RemoteException(message));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });
        }
    }
}
