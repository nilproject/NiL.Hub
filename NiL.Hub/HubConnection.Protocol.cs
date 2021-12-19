using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NiL.Exev;
using static NiL.Hub.Hub;

namespace NiL.Hub
{
    public sealed partial class HubConnection // Protocol
    {
        private void processReceived(List<Action> doAfter, long senderId, int packageSize)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            List<Exception> exceptions = null;
            var packageEnd = packageSize + (int)_inputBuffer.Position;
            while (_inputBuffer.Position < packageEnd)
            {
                try
                {
                    processSinglePackage(doAfter, senderId);
                }
                catch (Exception e)
                {
                    if (exceptions == null)
                        exceptions = new List<Exception>();

                    exceptions.Add(e);
                }
                finally
                {
                    _readSeqNumber++;
                }
            }

            if (exceptions != null)
                throw new AggregateException(exceptions);
        }

        private void processSinglePackage(List<Action> doAfter, long senderId)
        {
            var packageStart = (int)_inputBuffer.Position;
            var packageType = (PackageType)_inputBufferReader.ReadByte();
            switch (packageType)
            {
                case PackageType.ReadyForDisconnect:
                {
                    if (State == HubConnectionState.Disconnecting)
                    {
                        _reconnectOnFail = false;

                        doAfter.Add(onDisconnected);

                        return;
                    }

                    break;
                }

                case PackageType.Disconnect:
                {
                    _reconnectOnFail = false;

                    State = HubConnectionState.Disconnecting;
                    doAfter.Add(onDisconnected);

                    writeReadyForDisconnect();
                    break;
                }

                case PackageType.HelloResponse:
                case PackageType.Hello:
                {
                    var hubId = _inputBufferReader.ReadInt64();
                    var name = _inputBufferReader.ReadString();

                    if ((packageType == PackageType.Hello && State == HubConnectionState.NotInitialized)
                        || (packageType == PackageType.HelloResponse && State == HubConnectionState.HelloSent))
                    {
                        lock (_localHub._knownHubs)
                        {
                            if (!_localHub._knownHubs.TryGetValue(hubId, out var hub))
                                _localHub._knownHubs[hubId] = hub = new RemoteHub(new TypesMapLayer(_localHub._registeredInderfaces));

                            RemoteHub = hub;
                            hub.Id = hubId;
                            hub.Name = name;
                            hub._connections.Set(this, 0);
                        }

                        _allHubs.Add(hubId);
                        if (senderId == -1)
                            senderId = hubId;

                        if (packageType == PackageType.Hello)
                            writeHelloResponse();

                        if (_localHub.PathThrough)
                        {
                            // to this about all
                            forAllHubs((_, otherHub) =>
                            {
                                if (RemoteHub.Id != otherHub.Id)
                                {
                                    writeRegisterHub(otherHub);
                                }
                            });

                            doAfter.Add(() =>
                            {
                                // to all about this
                                forAllHubConnections(false, hubConn => // only those that are connected directly 
                                {
                                    hubConn.writeRegisterHub(RemoteHub);
                                    return true;
                                });
                            });
                        }

                        doAfter.Add(() =>
                        {
                            lock (_sync)
                            {
                                lock (_localHub._knownInterfaces)
                                {
                                    foreach (var @interface in _localHub._knownInterfaces)
                                    {
                                        var localReg = @interface.Value.LocalImplementation != null;
                                        if (!localReg && !_localHub.PathThrough)
                                            continue;

                                        var count = !_localHub.PathThrough ? 1 : @interface.Value.Hubs.Count + (localReg ? 1 : 0);

                                        var hubs = new long[count];
                                        var intIds = new uint[count];
                                        var versions = new int[count];

                                        if (_localHub.PathThrough)
                                        {
                                            for (var i = 0; i < @interface.Value.Hubs.Count; i++)
                                            {
                                                hubs[i] = @interface.Value.Hubs[i].Hub.Id;
                                                intIds[i] = @interface.Value.Hubs[i].InterfaceId;
                                                versions[i] = @interface.Value.Hubs[i].Version;
                                            }
                                        }

                                        if (localReg)
                                        {
                                            hubs[count - 1] = _localHub.Id;
                                            intIds[count - 1] = @interface.Value.LocalId;
                                            versions[count - 1] = @interface.Value.LocalVersion;
                                        }

                                        writeRegisterInterface(hubs, @interface.Key, intIds, versions);
                                    }

                                    FlushOutputBuffer();
                                }
                            }
                        });

                        //Thread.CurrentThread.Name = "Workder for connection from \"" + LocalHub.Name + "\" (" + LocalHub.Id + ") to \"" + RemoteHub.Name + "\" (" + RemoteHub.Id + ")";
                        State = HubConnectionState.Active;

                        onConnected();
                    }
                    else
                    {
                        writeError(ErrorCode.UnexpectedHello, "Unexpected hello");
                        FlushOutputBuffer();
                    }

                    break;
                }

                case PackageType.HubIsAvailable:
                {
                    var hubId = _inputBufferReader.ReadInt64();
                    var hubName = _inputBufferReader.ReadString();
                    var distance = _inputBufferReader.ReadInt32();

                    if (_allHubs.Contains(hubId))
                        break;

                    _allHubs.Add(hubId);

                    var hub = _localHub.HubIsAvailableThrough(this, hubId, distance, hubName);

                    if (_localHub.PathThrough)
                    {
                        doAfter.Add(() =>
                        {
                            forAllHubConnections(false, hubConn => // only those that are connected directly 
                            {
                                hubConn.writeRegisterHub(hub);
                                return true;
                            });
                        });
                    }

                    break;
                }

                case PackageType.HubIsUnavailable:
                {
                    var hubId = _inputBufferReader.ReadInt64();

                    lock (_localHub._knownHubs)
                    {
                        if (!_localHub._knownHubs.ContainsKey(hubId))
                            break;
                    }

                    _localHub.HubIsUnavailableThrough(this, hubId);

                    doAfter.Add(() =>
                    {
                        forAllHubConnections(false, hubConn => // only those that are connected directly
                        {
                            hubConn.writeUnRegisterHub(hubId);
                            return true;
                        });
                    });

                    break;
                }

                case PackageType.RegisterInterface:
                {
                    var interfaceName = _inputBufferReader.ReadString();
                    var hubsCount = (int)_inputBufferReader.ReadByte();

                    var hubs = new long[hubsCount];
                    var interfaceIds = new uint[hubsCount];
                    var versions = new int[hubsCount];
                    for (var i = 0; i < hubsCount; i++)
                    {
                        hubs[i] = _inputBufferReader.ReadInt64();
                        interfaceIds[i] = _inputBufferReader.ReadUInt32();
                        versions[i] = _inputBufferReader.ReadInt32();
                    }

                    lock (_localHub._knownInterfaces)
                    {
                        if (_localHub._knownInterfaces.TryGetValue(interfaceName, out var @interface))
                        {
                            var wPos = 0;
                            lock (@interface.Hubs)
                            {
                                for (var i = 0; i < hubs.Length; i++)
                                {
                                    var hubId = hubs[i];

                                    var knownHub = false;
                                    for (var j = 0; j < @interface.Hubs.Count && !knownHub; j++)
                                        knownHub |= @interface.Hubs[j].Hub.Id == hubId;

                                    if (!knownHub)
                                    {
                                        if (wPos != i)
                                        {
                                            hubs[wPos] = hubId;
                                            interfaceIds[wPos] = interfaceIds[i];
                                            versions[wPos] = versions[i];
                                        }

                                        wPos++;

                                        tryAddInterfaceToHubOrWriteError(@interface, hubId, interfaceIds[i], versions[i]);
                                    }
                                }
                            }

                            if (_localHub.PathThrough)
                            {
                                if (wPos == hubs.Length) // Все хабы новые. Нужно передать весь пакет дальше
                                {
                                    var pos = _inputBuffer.Position;
                                    _inputBuffer.Position = packageStart;
                                    var blob = _inputBufferReader.ReadBytes((int)(pos - packageStart));

                                    doAfter.Add(() =>
                                    {
                                        forAllHubConnections(false, connection =>
                                        {
                                            connection.writeBlob(blob);
                                            return true;
                                        });
                                    });
                                }
                                else if (wPos > 0) // О части хабов мы уже знали. Нужно сформировать новый пакет и передать дальше
                                {
                                    Array.Resize(ref hubs, wPos);
                                    Array.Resize(ref interfaceIds, wPos);
                                    Array.Resize(ref versions, wPos);
                                    doAfter.Add(() =>
                                    {
                                        forAllHubConnections(false, connection => // only those that are connected directly
                                        {
                                            connection.writeRegisterInterface(hubs, interfaceName, interfaceIds, versions);
                                            return true;
                                        });
                                    });
                                }
                            }
                        }
                        else // Интерфейс неизвестен. Нужно создать его и передать весь пакет как есть дальше
                        {
                            @interface = new SharedInterface(interfaceName);
                            for (var i = 0; i < hubs.Length; i++)
                                tryAddInterfaceToHubOrWriteError(@interface, hubs[i], interfaceIds[i], versions[i]);

                            _localHub._knownInterfaces.Add(interfaceName, @interface);

                            if (_localHub.PathThrough)
                            {
                                var pos = _inputBuffer.Position;
                                _inputBuffer.Position = packageStart;
                                var blob = _inputBufferReader.ReadBytes((int)(pos - packageStart));

                                doAfter.Add(() =>
                                {
                                    forAllHubConnections(false, connection =>
                                    {
                                        connection.writeBlob(blob);
                                        return true;
                                    });
                                });
                            }
                        }
                    }

                    break;
                }

                case PackageType.RetransmitTo:
                {
                    var receiverId = _inputBufferReader.ReadInt64();
                    var senderHubId = _inputBufferReader.ReadInt64();
                    var size = _inputBufferReader.ReadUInt16();

                    if (receiverId == _localHub.Id)
                    {
                        processReceived(doAfter, senderHubId, size);
                    }
                    else
                    {
                        if (!_localHub.PathThrough)
                        {
                            _inputBuffer.Position += size;
                            writeError(ErrorCode.UnknownHub, "Unknown hub #" + receiverId);
                            break;
                        }

                        var headerSize = (int)(_inputBuffer.Position - packageStart);
                        _inputBuffer.Position = packageStart;
                        var blob = _inputBufferReader.ReadBytes(size + headerSize);

                        doAfter.Add(() =>
                        {
                            RemoteHub receiverHub;
                            lock (_localHub._knownHubs)
                            {
                                if (!_localHub._knownHubs.TryGetValue(receiverId, out receiverHub))
                                {
                                    writeError(ErrorCode.UnknownHub, "Unknown hub #" + receiverId);
                                    return;
                                }
                            }

                            using var connection = receiverHub._connections.GetLockedConenction();
                            connection.Value.writeBlob(blob);
                            connection.Value.FlushOutputBuffer();
                        });
                    }

                    break;
                }

                case PackageType.Call:
                {
                    var awaitId = _inputBufferReader.ReadInt32();
                    var size = _inputBufferReader.ReadUInt16();
                    var code = _inputBufferReader.ReadBytes(size);

                    _localHub.Eval(senderId, awaitId, code);
                    break;
                }

                case PackageType.Result:
                {
                    var awaitId = _inputBufferReader.ReadInt32();
                    var size = _inputBufferReader.ReadUInt16();
                    var code = _inputBufferReader.ReadBytes(size);

                    _localHub.SetResult(awaitId, code);
                    break;
                }

                case PackageType.Exception:
                {
                    var awaitId = _inputBufferReader.ReadInt32();
                    var message = _inputBufferReader.ReadString();

                    _localHub.SetException(awaitId, message);
                    break;
                }

                case PackageType.Error:
                {
                    var errorCode = _inputBufferReader.ReadInt32();
                    var message = _inputBufferReader.ReadString();
                    // TODO
                    break;
                }

                case PackageType.Ping:
                {
                    writePong();
                    break;
                }

                case PackageType.Pong:
                {
                    break;
                }

                case PackageType.StreamGetInfo:
                {
                    var streamId = _inputBufferReader.ReadInt32();

                    doAfter.Add(() =>
                    {
                        RemoteHub remoteHub;
                        lock (_localHub._knownHubs)
                        {
                            if (!_localHub._knownHubs.TryGetValue(senderId, out remoteHub))
                            {
                                writeError(ErrorCode.UnknownHub, "Unknown hub #" + senderId);
                                return;
                            }
                        }

                        using var connection = remoteHub._connections.GetLockedConenction();
                        var got = false;
                        var stream = default(RegisteredStream);
                        lock (_localHub._streams)
                        {
                            got = _localHub._streams.TryGetValue(streamId, out stream);
                        }

                        if (got)
                        {
                            connection.Value.WriteRetransmitTo(
                                senderId,
                                _ =>
                                {
                                    connection.Value.WriteStreamInfo(
                                        streamId,
                                        stream.Stream.Length,
                                        stream.Stream.Position,
                                        stream.Stream.CanSeek,
                                        stream.Stream.CanWrite,
                                        stream.Stream.CanRead);
                                });
                        }
                        else
                        {
                            connection.Value.WriteRetransmitTo(
                                   senderId,
                                   _ =>
                                   {
                                       connection.Value.WriteStreamInfo(streamId, 0, 0, false, false, false);
                                   });
                        }

                        connection.Value.FlushOutputBuffer();
                    });

                    break;
                }

                case PackageType.StreamInfo:
                {
                    var streamId = _inputBufferReader.ReadInt32();
                    var length = _inputBufferReader.ReadInt64();
                    var position = _inputBufferReader.ReadInt64();
                    var flags = _inputBufferReader.ReadByte();

                    TaskCompletionSource<RemoteStream> remoteStreamTask;
                    lock (_localHub._remoteStreams)
                    {
                        _localHub._remoteStreams.TryGetValue((senderId, streamId), out remoteStreamTask);
                    }

                    doAfter.Add(() =>
                    {
                        if (remoteStreamTask != null)
                        {
                            if ((flags & 3) != 0)
                            {
                                if (remoteStreamTask.Task.IsCompleted)
                                {
                                    var stream = remoteStreamTask.Task.Result;
                                    stream.SetCanRead((flags & 1) != 0);
                                    stream.SetCanWrite((flags & 2) != 0);
                                    stream.SetCanSeek((flags & 4) != 0);
                                    stream.SetPosition(position);
                                    stream.SetLengthInternal(length);
                                }
                                else
                                {
                                    remoteStreamTask
                                        .SetResult(
                                            new RemoteStream(
                                                _localHub,
                                                _localHub._knownHubs[senderId],
                                                streamId,
                                                length,
                                                position,
                                                (flags & 1) != 0,
                                                (flags & 2) != 0,
                                                (flags & 4) != 0));
                                }
                            }
                            else
                            {
                                remoteStreamTask.SetException(new KeyNotFoundException("Unknown stream"));
                            }
                        }
                    });

                    break;
                }

                case PackageType.StreamRead:
                {
                    var streamId = _inputBufferReader.ReadInt32();
                    var count = _inputBufferReader.ReadUInt16();

                    doAfter.Add(() =>
                    {
                        RemoteHub remoteHub;
                        lock (_localHub._knownHubs)
                        {
                            if (!_localHub._knownHubs.TryGetValue(senderId, out remoteHub))
                            {
                                writeError(ErrorCode.UnknownHub, "Unknown hub #" + senderId);
                                return;
                            }
                        }

                        using var connection = remoteHub._connections.GetLockedConenction();
                        lock (_localHub._streams)
                        {
                            if (_localHub._streams.TryGetValue(streamId, out var stream))
                            {
                                var buffer = new byte[count];
                                var r = stream.Stream.Read(buffer);

                                connection.Value.WriteRetransmitTo(
                                           senderId,
                                           _ =>
                                           {
                                               connection.Value.WriteStreamInfo(streamId,
                                                                        stream.Stream.Length,
                                                                        stream.Stream.Position,
                                                                        stream.Stream.CanSeek,
                                                                        stream.Stream.CanWrite,
                                                                        stream.Stream.CanRead);
                                               connection.Value.WriteStreamData(streamId, new Span<byte>(buffer, 0, r));
                                           });
                            }
                            else
                            {
                                connection.Value.WriteRetransmitTo(
                                           senderId,
                                           _ =>
                                           {
                                               connection.Value.WriteStreamData(streamId, Array.Empty<byte>());
                                           });
                            }
                        }

                        connection.Value.FlushOutputBuffer();
                    });

                    break;
                }

                case PackageType.StreamData:
                {
                    var streamId = _inputBufferReader.ReadInt32();
                    var count = _inputBufferReader.ReadUInt16();
                    var data = _inputBufferReader.ReadBytes(count);

                    TaskCompletionSource<RemoteStream> remoteStreamTask;
                    lock (_localHub._remoteStreams)
                    {
                        _localHub._remoteStreams.TryGetValue((senderId, streamId), out remoteStreamTask);
                    }

                    if (remoteStreamTask != null && remoteStreamTask.Task.IsCompleted)
                    {
                        var stream = remoteStreamTask.Task.Result;
                        stream.ReceiveData(data);
                    }

                    break;
                }

                case PackageType.StreamSeek:
                {
                    var streamId = _inputBufferReader.ReadInt32();
                    var offset = _inputBufferReader.ReadInt64();
                    var origin = (SeekOrigin)_inputBufferReader.ReadByte();

                    doAfter.Add(() =>
                    {
                        RemoteHub remoteHub;
                        lock (_localHub._knownHubs)
                        {
                            if (!_localHub._knownHubs.TryGetValue(senderId, out remoteHub))
                            {
                                writeError(ErrorCode.UnknownHub, "Unknown hub #" + senderId);
                                return;
                            }
                        }

                        using var connection = remoteHub._connections.GetLockedConenction();
                        var got = false;
                        var regStream = default(RegisteredStream);
                        lock (_localHub._streams)
                        {
                            got = _localHub._streams.TryGetValue(streamId, out regStream);
                        }

                        if (got)
                        {
                            var stream = regStream.Stream;
                            stream.Seek(offset, origin);

                            connection.Value.WriteRetransmitTo(
                                       senderId,
                                       _ =>
                                       {
                                           connection.Value.WriteStreamInfo(
                                                streamId,
                                                stream.Length,
                                                stream.Position,
                                                stream.CanSeek,
                                                stream.CanWrite,
                                                stream.CanRead);
                                           connection.Value.WriteStreamData(streamId, Array.Empty<byte>());
                                       });
                        }
                        else
                        {
                            connection.Value.WriteRetransmitTo(
                                          senderId,
                                          _ =>
                                          {
                                              connection.Value.WriteStreamData(streamId, Array.Empty<byte>());
                                          });
                        }


                        connection.Value.FlushOutputBuffer();
                    });

                    break;
                }

                case PackageType.StreamSetLength:
                {
                    var streamId = _inputBufferReader.ReadInt32();
                    var newLength = _inputBufferReader.ReadInt64();

                    doAfter.Add(() =>
                    {
                        RemoteHub remoteHub;
                        lock (_localHub._knownHubs)
                        {
                            if (!_localHub._knownHubs.TryGetValue(senderId, out remoteHub))
                            {
                                writeError(ErrorCode.UnknownHub, "Unknown hub #" + senderId);
                                return;
                            }
                        }

                        using var connection = remoteHub._connections.GetLockedConenction();
                        var got = false;
                        var regStream = default(RegisteredStream);
                        lock (_localHub._streams)
                        {
                            got = _localHub._streams.TryGetValue(streamId, out regStream);
                        }

                        if (got)
                        {
                            var stream = regStream.Stream;
                            stream.SetLength(newLength);

                            connection.Value.WriteRetransmitTo(
                                       senderId,
                                       _ =>
                                       {
                                           connection.Value.WriteStreamInfo(streamId, stream.Length, stream.Position, stream.CanSeek, stream.CanWrite, stream.CanRead);
                                           connection.Value.WriteStreamData(streamId, Array.Empty<byte>());
                                       });
                        }
                        else
                        {
                            connection.Value.WriteRetransmitTo(
                                          senderId,
                                          _ =>
                                          {
                                              connection.Value.WriteStreamData(streamId, Array.Empty<byte>());
                                          });
                        }

                        connection.Value.FlushOutputBuffer();
                    });

                    break;
                }

                case PackageType.StreamWrite:
                {
                    var streamId = _inputBufferReader.ReadInt32();
                    var count = _inputBufferReader.ReadUInt16();
                    var data = _inputBufferReader.ReadBytes(count);

                    doAfter.Add(() =>
                    {
                        RemoteHub remoteHub;
                        lock (_localHub._knownHubs)
                        {
                            if (!_localHub._knownHubs.TryGetValue(senderId, out remoteHub))
                            {
                                writeError(ErrorCode.UnknownHub, "Unknown hub #" + senderId);
                                return;
                            }
                        }

                        using var connection = remoteHub._connections.GetLockedConenction();
                        var got = false;
                        var regStream = default(RegisteredStream);
                        lock (_localHub._streams)
                        {
                            got = _localHub._streams.TryGetValue(streamId, out regStream);
                        }

                        if (got)
                        {
                            var stream = regStream.Stream;
                            stream.Write(data);

                            connection.Value.WriteRetransmitTo(
                                       senderId,
                                       _ =>
                                       {
                                           connection.Value.WriteStreamInfo(streamId, stream.Length, stream.Position, stream.CanSeek, stream.CanWrite, stream.CanRead);
                                           connection.Value.WriteStreamData(streamId, Array.Empty<byte>());
                                       });
                        }
                        else
                        {
                            connection.Value.WriteRetransmitTo(
                                          senderId,
                                          _ =>
                                          {
                                              connection.Value.WriteStreamData(streamId, Array.Empty<byte>());
                                          });
                        }

                        connection.Value.FlushOutputBuffer();
                    });

                    break;
                }

                case PackageType.StreamClose:
                {
                    var streamId = _inputBufferReader.ReadInt32();

                    lock (_localHub._streams)
                    {
                        if (_localHub._streams.TryGetValue(streamId, out var stream))
                        {
                            stream.Stream.Close();

                            _localHub._streams.Remove(streamId);
                        }
                    }

                    break;
                }

                default: throw new NotImplementedException($"Support of package {packageType} not implemented yet");
            }
        }

        private void tryAddInterfaceToHubOrWriteError(SharedInterface @interface, long hubId, uint interfaceInHubId, int version)
        {
            RemoteHub hub;
            lock (_localHub._knownHubs)
            {
                if (!_localHub._knownHubs.TryGetValue(hubId, out hub))
                {
                    writeError(ErrorCode.UnknownHub, "Unknown hub #" + hubId);
                    return;
                }
            }

            @interface.Hubs.Add(new RemoteHubInterfaceLink(hub, interfaceInHubId, version));
            lock (hub._interfaces)
                hub._interfaces.Add(@interface.Name);
        }

        private void invalidateConnection()
        {
            List<long> unavailableHubs = null;
            foreach (var hubId in _allHubs)
            {
                _localHub.HubIsUnavailableThrough(this, hubId);
                if (!_localHub._knownHubs.ContainsKey(hubId))
                {
                    if (unavailableHubs == null)
                        unavailableHubs = new List<long>();

                    unavailableHubs.Add(hubId);
                }
            }

            if (unavailableHubs != null)
            {
                forAllHubConnections(false, hubConn => // only those that are connected directly 
                {
                    foreach (var hubId in unavailableHubs)
                        hubConn.writeUnRegisterHub(hubId);

                    return true;
                });
            }

            _allHubs.Clear();

            lock (_localHub._hubsConnctions)
            {
                if (_localHub._hubsConnctions.TryGetValue(IPEndPoint, out var connections))
                {
                    connections.Remove(this);

                    if (connections.Count == 0)
                        _localHub._hubsConnctions.Remove(IPEndPoint);
                }
            }
        }

        private void forAllHubConnections(bool includeThisHubId, Func<HubConnection, bool> action)
        {
            HubConnection[] hubs;
            lock (_localHub._hubsConnctions)
                hubs = _localHub._hubsConnctions.Values.SelectMany(x => x).ToArray();

            var currentRemoteHubId = RemoteHub.Id;
            foreach (var hubConnection in hubs)
            {
                if (hubConnection.State != HubConnectionState.Active)
                    continue;

                if (!includeThisHubId && hubConnection.RemoteHub.Id == currentRemoteHubId)
                    continue;

                lock (hubConnection._sync)
                {
                    if (action(hubConnection))
                        hubConnection.FlushOutputBuffer();
                }
            }
        }

        private void forAllHubs(Action<HubConnection, RemoteHub> action)
        {
            List<RemoteHub> othersHubs;
            lock (_localHub._knownHubs)
                othersHubs = _localHub._knownHubs.Values.ToList();

            foreach (var hub in othersHubs)
            {
                using var connection = hub._connections.GetLockedConenction();
                if (connection != null)
                {
                    action(connection.Value, hub);
                    connection.Value.FlushOutputBuffer();
                }
            }
        }

        /*private void forAllHubs(Action<HubConnection, RemoteHub> action)
        {
            List<HubConnection> hubConnections;
            lock (LocalHub._hubsConnctions)
                hubConnections = LocalHub._hubsConnctions.Values.Select(x => x[0]).ToList();

            var processedHubs = new HashSet<long>();

            foreach (var hubConnection in hubConnections)
            {
                if (hubConnection.State == HubConnectionState.Active)
                {
                    lock (hubConnection._sync)
                    {
                        foreach (var hubId in hubConnection.AllHubs)
                        {
                            if (processedHubs.Contains(hubId))
                                continue;

                            try
                            {
                                RemoteHub hub;
                                lock (LocalHub._knownHubs)
                                    if (!LocalHub._knownHubs.TryGetValue(hubId, out hub))
                                        continue;

                                lock (hub)
                                {
                                    if (hub._shortestPath.HubConnection != hubConnection)
                                        continue;

                                    action(hubConnection, hub);

                                    processedHubs.Add(hubId);
                                }
                            }
                            catch
                            {
                                hubConnection._outputBuffer.SetLength(0);
                                throw;
                            }
                        }

                        hubConnection.flushOutputBuffer();
                    }
                }
            }
        }*/

        private void writeHello()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.Hello);
            _outputBufferWritter.Write(_localHub.Id);
            _outputBufferWritter.Write(_localHub.Name);

            _writeSeqNumber++;
        }

        private void writeHelloResponse()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.HelloResponse);
            _outputBufferWritter.Write(_localHub.Id);
            _outputBufferWritter.Write(_localHub.Name);

            _writeSeqNumber++;
        }

        private void writeDisconnect()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.Disconnect);

            _writeSeqNumber++;
        }

        private void writeReadyForDisconnect()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.ReadyForDisconnect);

            _writeSeqNumber++;
        }

        private void writeError(ErrorCode errorCode, string message)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.Error);
            _outputBufferWritter.Write((int)errorCode);
            _outputBufferWritter.Write(message);

            _writeSeqNumber++;
        }

        private void writeRegisterHub(RemoteHub hubToRegistration)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.HubIsAvailable);
            _outputBufferWritter.Write(hubToRegistration.Id);
            _outputBufferWritter.Write(hubToRegistration.Name);
            _outputBufferWritter.Write(hubToRegistration._connections.GetShortedDistance() + 1);

            _writeSeqNumber++;
        }

        private void writeUnRegisterHub(long hubToUnRegistrationId)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.HubIsUnavailable);
            _outputBufferWritter.Write(hubToUnRegistrationId);

            _writeSeqNumber++;
        }

        internal void WriteRetransmitTo(long hubId, Action<HubConnection> nestedDataWritter)
        {
            if (nestedDataWritter is null)
                throw new ArgumentNullException(nameof(nestedDataWritter));

            Debug.Assert(Monitor.IsEntered(_sync));

            var startPos = _outputBuffer.Position;

            _outputBufferWritter.Write((byte)PackageType.RetransmitTo);
            _outputBufferWritter.Write(hubId);
            _outputBufferWritter.Write(_localHub.Id);
            _outputBufferWritter.Write((ushort)0);

            var pos = _outputBuffer.Position;

            nestedDataWritter(this);

            var size = _outputBuffer.Position - pos;

            if (size > ushort.MaxValue)
            {
                _outputBuffer.Position = startPos;
                throw new InvalidOperationException("Package is too large");
            }

            _outputBuffer.Position = pos - 2;
            _outputBufferWritter.Write((ushort)size);
            _outputBuffer.Position += size;

            _writeSeqNumber++;
        }

        internal long WriteRegisterInterface(long hubId, string interfaceName, uint interfaceId, int version)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.RegisterInterface);
            _outputBufferWritter.Write(interfaceName);
            _outputBufferWritter.Write((byte)1);
            _outputBufferWritter.Write(hubId);
            _outputBufferWritter.Write(interfaceId);
            _outputBufferWritter.Write(version);

            return _writeSeqNumber++;
        }

        private void writeRegisterInterface(long[] hubs, string interfaceName, uint[] interfaceIds, int[] versions)
        {
            Debug.Assert(Monitor.IsEntered(_sync));
            Debug.Assert(hubs.Length == interfaceIds.Length);

            if (hubs.Length > byte.MaxValue)
                throw new ArgumentOutOfRangeException("Size of " + nameof(hubs) + " is greater than " + byte.MaxValue);

            if (hubs.Length != interfaceIds.Length)
                throw new ArgumentException("Size of " + nameof(hubs) + " not equals to " + nameof(interfaceIds));

            var hubsCount = (byte)hubs.Length;

            _outputBufferWritter.Write((byte)PackageType.RegisterInterface);
            _outputBufferWritter.Write(interfaceName);
            _outputBufferWritter.Write(hubsCount);
            for (var i = 0; i < hubsCount; i++)
            {
                _outputBufferWritter.Write(hubs[i]);
                _outputBufferWritter.Write(interfaceIds[i]);
                _outputBufferWritter.Write(versions[i]);
            }

            _writeSeqNumber++;
        }

        internal void WriteCall(int resultAwaitId, byte[] serializedExpression)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            if (serializedExpression.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("Size of " + nameof(serializedExpression) + " is greater than " + ushort.MaxValue);

            _outputBufferWritter.Write((byte)PackageType.Call);
            _outputBufferWritter.Write(resultAwaitId);
            _outputBufferWritter.Write((ushort)serializedExpression.Length);
            _outputBuffer.Write(serializedExpression);
        }

        internal void WriteResult(int resultAwaitId, ReadOnlySpan<byte> serializedExpression)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            if (serializedExpression.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("Size of " + nameof(serializedExpression) + " is greater than " + ushort.MaxValue);

            _outputBufferWritter.Write((byte)PackageType.Result);
            _outputBufferWritter.Write(resultAwaitId);
            _outputBufferWritter.Write((ushort)serializedExpression.Length);
            _outputBuffer.Write(serializedExpression);
        }

        internal void WriteException(int resultAwaitId, Exception e)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.Exception);
            _outputBufferWritter.Write(resultAwaitId);
            _outputBufferWritter.Write(e.GetType().FullName + ": " + e.Message);
        }

        internal void WriteStreamGetInfo(int streamId)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.StreamGetInfo);
            _outputBufferWritter.Write(streamId);
        }

        internal void WriteStreamInfo(int streamId, long length, long position, bool canSeek, bool canWrite, bool canRead)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            var flags =
                (canRead ? 1 : 0)
                | (canWrite ? 2 : 0)
                | (canSeek ? 4 : 0);

            _outputBufferWritter.Write((byte)PackageType.StreamInfo);
            _outputBufferWritter.Write(streamId);
            _outputBufferWritter.Write(length);
            _outputBufferWritter.Write(position);
            _outputBufferWritter.Write((byte)flags);
        }

        internal void WriteStreamRead(int streamId, ushort length)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.StreamRead);
            _outputBufferWritter.Write(streamId);
            _outputBufferWritter.Write(length);
        }

        internal void WriteStreamWrite(int streamId, ReadOnlySpan<byte> data)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.StreamWrite);
            _outputBufferWritter.Write(streamId);
            _outputBufferWritter.Write((ushort)data.Length);
            _outputBufferWritter.Write(data);
        }

        internal void WriteStreamSeek(int streamId, long offset, SeekOrigin origin)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.StreamSeek);
            _outputBufferWritter.Write(streamId);
            _outputBufferWritter.Write((long)offset);
            _outputBufferWritter.Write((byte)origin);
        }

        internal void WriteStreamSetLength(int streamId, long newLength)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.StreamSetLength);
            _outputBufferWritter.Write(streamId);
            _outputBufferWritter.Write((ulong)newLength);
        }

        internal void WriteStreamClose(int streamId)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.StreamClose);
            _outputBufferWritter.Write(streamId);
        }

        private void WriteStreamData(int streamId, Span<byte> buffer)
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.StreamData);
            _outputBufferWritter.Write(streamId);
            _outputBufferWritter.Write((ushort)buffer.Length);
            _outputBufferWritter.Write(buffer);
        }

        private void writeBlob(Span<byte> buffer)
        {
            _outputBuffer.Write(buffer);
        }

        private void writePing()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.Ping);
        }

        private void writePong()
        {
            Debug.Assert(Monitor.IsEntered(_sync));

            _outputBufferWritter.Write((byte)PackageType.Pong);
        }
    }
}
