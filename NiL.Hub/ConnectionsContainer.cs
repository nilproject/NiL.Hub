using System;
using System.Collections.Generic;

namespace NiL.Hub
{
    internal sealed class ConnectionsContainer
    {
        private readonly Dictionary<HubConnection, int> _distancesByConnections = new Dictionary<HubConnection, int>();
        private readonly List<List<HubConnection>> _connectionsByDistance = new List<List<HubConnection>>();

        public int Count => _distancesByConnections.Count;

        public void Set(HubConnection hubConnection, int distance)
        {
            lock (_distancesByConnections)
            {
                if (_distancesByConnections.TryGetValue(hubConnection, out var curDist))
                    _connectionsByDistance[curDist].Remove(hubConnection);

                while (_connectionsByDistance.Count <= distance)
                    _connectionsByDistance.Add(null);

                if (_connectionsByDistance[distance] == null)
                    _connectionsByDistance[distance] = new List<HubConnection>();

                _connectionsByDistance[distance].Add(hubConnection);
                _distancesByConnections.Add(hubConnection, distance);
            }
        }

        public void Remove(HubConnection connection)
        {
            lock (_distancesByConnections)
            {
                if (!_distancesByConnections.TryGetValue(connection, out var distance))
                    return;

                _distancesByConnections.Remove(connection);
                _connectionsByDistance[distance].Remove(connection);
            }
        }

        public Locked<HubConnection> GetLockedConenction()
        {
            HubConnection any = null;
            lock (_distancesByConnections)
            {
                for (var i = 0; i < _connectionsByDistance.Count; i++)
                {
                    var connections = _connectionsByDistance[i];

                    if (connections == null)
                        continue;

                    var offset = Environment.TickCount % connections.Count;

                    for (var j = 0; j < connections.Count; j++)
                    {
                        if (connections[j].State != HubConnectionState.Active)
                            continue;

                        if (any == null)
                            any = connections[j];

                        var index = (offset + j) % connections.Count;
                        if (connections[index].TryGetLocked(out var connection))
                        {
                            var secondIndex = (index + (index >> 1)) % connections.Count;
                            if (secondIndex != index)
                            {
                                var t = connections[index];
                                connections[index] = connections[secondIndex];
                                connections[secondIndex] = t;
                            }

                            return connection;
                        }
                    }
                }
            }

            return any?.GetLocked();
        }

        public int GetShortedDistance()
        {
            lock (_distancesByConnections)
            {
                for (var i = 0; i < _connectionsByDistance.Count; i++)
                {
                    var connections = _connectionsByDistance[i];

                    if (connections == null)
                        continue;

                    return i;
                }

                return int.MaxValue;
            }
        }
    }
}
