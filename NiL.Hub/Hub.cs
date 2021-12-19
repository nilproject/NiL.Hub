using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NiL.Exev;

namespace NiL.Hub
{
    public partial class Hub : IHub
    {
        private sealed class ToArrayTools
        {
            public Func<IList> NewList;
            public Func<IList, object> ToArray;
        }

        internal sealed class RegisteredStream
        {
            public int LastUse { get; private set; } = Environment.TickCount;

            private Stream _stream;
            public Stream Stream
            {
                get
                {
                    LastUse = Environment.TickCount;
                    return _stream;
                }

                init => _stream = value;
            }
        }

        private uint _interfaceIdCounter;
        private volatile int _callResultAwaitId;
        private volatile int _streamsId;

        private readonly Dictionary<Type, ToArrayTools> _toArrayTools = new Dictionary<Type, ToArrayTools>();

        internal readonly Dictionary<MemberInfo, bool> _accessCache = new Dictionary<MemberInfo, bool>();
        internal readonly TypesMapLayer _registeredInderfaces = new TypesMapLayer();
        internal readonly Dictionary<string, SharedInterface> _knownInterfaces = new Dictionary<string, SharedInterface>();
        internal readonly Dictionary<IPEndPoint, Thread> _listeners = new Dictionary<IPEndPoint, Thread>();
        internal readonly Dictionary<IPEndPoint, List<HubConnection>> _hubsConnctions = new Dictionary<IPEndPoint, List<HubConnection>>();
        internal readonly Dictionary<long, RemoteHub> _knownHubs = new Dictionary<long, RemoteHub>();
        internal readonly Dictionary<int, RegisteredStream> _streams = new Dictionary<int, RegisteredStream>();
        internal readonly Dictionary<(long, int), TaskCompletionSource<RemoteStream>> _remoteStreams = new Dictionary<(long, int), TaskCompletionSource<RemoteStream>>();
        internal readonly ExpressionEvaluator _expressionEvaluator;

        internal readonly ExpressionDeserializer _expressionDeserializer;
        internal readonly Dictionary<int, TaskCompletionSource<object>> _awaiters = new Dictionary<int, TaskCompletionSource<object>>();

        [ThreadStatic]
        internal static Hub _currentHub;

        public IEnumerable<RemoteHub> KnownHubs => _knownHubs.Values;
        public IEnumerable<HubConnection> Connections => _hubsConnctions.Values.SelectMany(x => x);
        public IEnumerable<IPEndPoint> EndPoints => _listeners.Count > 0 ? _listeners.Select(x => x.Key) : default;

        public bool PathThrough { get; set; }

        public Hub()
            : this("<unnamed hub >")
        { }

        public Hub(string name)
            : this((uint)(name.GetHashCode() ^ DateTime.Now.GetHashCode() ^ Environment.TickCount), name)
        { }

        public Hub(long id, string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = id;

            _registeredInderfaces = new TypesMapLayer();
            _expressionDeserializer = new ExpressionDeserializer(_registeredInderfaces);

            _expressionEvaluator = new ExpressionEvaluator(validateAccess);
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
                            HubConnection.AcceptConnected(this, tcpClient, false);
                        }
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        public Task<IHubConnection> Connect(IPEndPoint endPoint, bool autoReconnect = true)
        {
            return Task.Run(() => HubConnection.Connect(this, endPoint, autoReconnect) as IHubConnection);
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
                    hub = new RemoteHub(new TypesMapLayer(_registeredInderfaces))
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

        private bool validateAccess(MemberInfo member)
        {
            lock (_accessCache)
            {
                if (_accessCache.TryGetValue(member, out var result))
                    return result;

                _accessCache[member] = result = !member.IsDefined(typeof(DenyRemoteCallAttribute));

                return result;
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

        public Task RegisterInterface<TInterface>(TInterface implementation, int version = default) where TInterface : class
        {
            return Task.Run(() =>
            {
                var interfaceName = typeof(TInterface).FullName;
                uint interfaceId;

                lock (_knownInterfaces)
                {
                    interfaceId = ++_interfaceIdCounter;

                    if (_knownInterfaces.TryGetValue(interfaceName, out var knownInterface))
                    {
                        if (knownInterface.LocalImplementation != null)
                            throw new InvalidOperationException("\"" + interfaceName + "\" already has local registration");
                    }
                    else
                    {
                        _knownInterfaces[interfaceName] = knownInterface = new SharedInterface<TInterface>(this, interfaceName)
                        {
                            LocalId = interfaceId,
                            LocalVersion = version
                        };
                    }

                    knownInterface.LocalImplementation = implementation;
                    _registeredInderfaces.Add(typeof(TInterface), interfaceId);
                }

                lock (_hubsConnctions)
                {
                    foreach (var connections in _hubsConnctions)
                    {
                        using var connection = connections.Value[0].GetLocked();
                        connection.Value.WriteRegisterInterface(Id, interfaceName, interfaceId, version);
                        connection.Value.FlushOutputBuffer();
                    }
                }
            });
        }

        public Task UnRegisterInterface<TInterface>() where TInterface : class
        {
            throw new NotImplementedException();
        }

        public int RegisterStream(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            lock (_streams)
            {
                var id = Interlocked.Increment(ref _streamsId);
                _streams.Add(id, new RegisteredStream { Stream = stream });
                return id;
            }
        }

        public Task<RemoteStream> GetRemoteStream(long hubId, int streamId)
        {
            TaskCompletionSource<RemoteStream> remoteStream;
            var key = (hubId, streamId);
            try
            {
                if (!_knownHubs.TryGetValue(hubId, out var hub))
                    throw new InvalidOperationException("Hub " + hubId + " is unknown");

                lock (_remoteStreams)
                {
                    if (_remoteStreams.TryGetValue((hubId, streamId), out remoteStream))
                        return remoteStream.Task;

                    remoteStream = new TaskCompletionSource<RemoteStream>();
                    _remoteStreams[key] = remoteStream;
                }

                using var connection = hub._connections.GetLockedConenction();
                connection.Value.WriteRetransmitTo(hubId, x =>
                {
                    x.WriteStreamGetInfo(streamId);
                });

                connection.Value.FlushOutputBuffer();

                return remoteStream.Task;
            }
            catch
            {
                lock (_remoteStreams)
                    _remoteStreams.Remove(key);
                throw;
            }
        }

        public bool UnregisterStream(int streamId)
        {
            lock (_streams)
            {
                return _streams.Remove(streamId);
            }
        }

        internal TaskCompletionSource<object> AllocTaskCompletionSource(out int taskAwaitId)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            lock (_awaiters)
            {
                taskAwaitId = Interlocked.Increment(ref _callResultAwaitId);
                _awaiters.Add(taskAwaitId, taskCompletionSource);
            }

            return taskCompletionSource;
        }

        internal Task Eval(long hubId, int awaitId, byte[] code)
        {
            var resultTask = new TaskCompletionSource<object>();

            var task = Task.Run(() =>
            {
                try
                {
                    doEval(hubId, awaitId, code, resultTask);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            });

            return resultTask.Task;
        }

        private void doEval(long hubId, int awaitId, byte[] code, TaskCompletionSource<object> resultTask)
        {
            var remoteHub = getRemoteHub(hubId);

            var oldCurrentHub = _currentHub;
            _currentHub = this;

            try
            {
                var deserialized = (LambdaExpression)_expressionDeserializer.Deserialize(code, Array.Empty<ParameterExpression>());
                if (deserialized.Parameters.Count != 1)
                    throw new ArgumentException("Number of parameters is not equal to 1");

                object implementation = GetLocalImplementation(deserialized.Parameters[0].Type);
                var result = _expressionEvaluator.Eval(deserialized.Body, new Parameter(deserialized.Parameters[0], implementation));

                void sendResult()
                {
                    using var connection = remoteHub._connections.GetLockedConenction();

                    connection.Value.WriteRetransmitTo(hubId, c =>
                    {
                        c.WriteResult(awaitId, remoteHub._expressionSerializer.Serialize(Expression.Constant(result)));
                    });

                    connection.Value.FlushOutputBuffer();

                    resultTask.SetResult(result);
                }

                var type = deserialized.ReturnType;
                void unwrapAndSend()
                {
                    if (result != null && type.IsAssignableTo(typeof(Task)))
                    {
                        var awaiterMethod = type.GetMethod("GetAwaiter");
                        var awaiter = (ICriticalNotifyCompletion)_expressionEvaluator.Eval(Expression.Call(Expression.Constant(result), awaiterMethod));
                        if (awaiterMethod.ReturnType.IsGenericType)
                        {
                            var getResult = awaiter.GetType().GetMethod(nameof(TaskAwaiter.GetResult));
                            var expr = Expression.Call(Expression.Constant(awaiter), getResult);
                            awaiter.UnsafeOnCompleted(() =>
                            {
                                try
                                {
                                    type = getResult.ReturnType;
                                    result = _expressionEvaluator.Eval(expr);
                                    unwrapAndSend();
                                }
                                catch (Exception e)
                                {
                                    using var connection = remoteHub._connections.GetLockedConenction();
                                    connection.Value.WriteRetransmitTo(hubId, c => c.WriteException(awaitId, e));
                                    connection.Value.FlushOutputBuffer();

                                    resultTask.SetException(e);

                                    Console.Error.WriteLine(e.Message);
                                }
                            });
                        }
                        else
                        {
                            awaiter.UnsafeOnCompleted(() =>
                            {
                                result = null;

                                try
                                {
                                    sendResult();
                                }
                                catch (Exception e)
                                {
                                    Console.Error.WriteLine(e);
                                }
                            });
                        }
                    }
                    else
                    {
                        if (!(result is string) && (result is IEnumerable enumerable))
                        {
                            if (!result.GetType().IsArray)
                            {
                                var interfaces = result.GetType().GetInterfaces();
                                Type @interface = null;
                                for (var i = 0; i < interfaces.Length; i++)
                                {
                                    if ((interfaces[i].FullName.StartsWith(typeof(IEnumerable).FullName) || interfaces[i].FullName.StartsWith(typeof(IEnumerable<>).FullName))
                                        && (@interface == null || interfaces[i].FullName.Length > @interface.FullName.Length))
                                    {
                                        @interface = interfaces[i];
                                    }
                                }

                                var itemType = @interface.IsConstructedGenericType ? @interface.GetGenericArguments()[0] : typeof(object);

                                if (itemType == typeof(object))
                                {
                                    var list = new List<object>();
                                    foreach (var item in enumerable)
                                    {
                                        list.Add(item);
                                    }

                                    result = list.ToArray();
                                }
                                else
                                {
                                    var listType = typeof(List<>).MakeGenericType(itemType);

                                    if (!_toArrayTools.TryGetValue(itemType, out var arrayTools))
                                    {
                                        var listPrm = Expression.Parameter(typeof(IList));
                                        arrayTools = new ToArrayTools
                                        {
                                            NewList = Expression.Lambda<Func<IList>>(Expression.New(listType)).Compile(),
                                            ToArray = Expression.Lambda<Func<IList, object>>(
                                                Expression.Call(Expression.Convert(listPrm, listType), listType.GetMethod("ToArray")), listPrm)
                                            .Compile(),
                                        };
                                    }

                                    var list = arrayTools.NewList();
                                    foreach (var item in enumerable)
                                    {
                                        list.Add(item);
                                    }

                                    result = arrayTools.ToArray(list);
                                }
                            }
                        }

                        sendResult();
                    }
                }

                unwrapAndSend();
            }
            catch (Exception e)
            {
                using var connection = remoteHub._connections.GetLockedConenction();
                connection.Value.WriteRetransmitTo(hubId, c => c.WriteException(awaitId, e));
                connection.Value.FlushOutputBuffer();

                resultTask.SetException(e);

                Console.Error.WriteLine(e.Message);
            }
            finally
            {
                _currentHub = oldCurrentHub;
            }
        }

        private TaskCompletionSource<object> getAwaiter(int awaitId)
        {
            TaskCompletionSource<object> awaiter;
            var knownAwaiter = false;
            lock (_awaiters)
            {
                knownAwaiter = _awaiters.TryGetValue(awaitId, out awaiter);
                if (knownAwaiter)
                    _awaiters.Remove(awaitId);
            }

            if (!knownAwaiter)
            {
                Console.Error.WriteLine("Unknown awaiter #" + awaitId);
                return awaiter;
            }

            return awaiter;
        }

        internal Task SetResult(int awaitId, byte[] code)
        {
            return Task.Run(() =>
            {
                var awaiter = getAwaiter(awaitId);

                try
                {
                    var deserialized = _expressionDeserializer.Deserialize(code, Array.Empty<ParameterExpression>());
                    var value = _expressionEvaluator.Eval(deserialized, Array.Empty<Parameter>());

                    awaiter.SetResult(value);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    awaiter.SetException(e);
                }
            });
        }

        internal Task SetException(int awaitId, string message)
        {
            return Task.Run(() =>
            {
                try
                {
                    var awaiter = getAwaiter(awaitId);

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
