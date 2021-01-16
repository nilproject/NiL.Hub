using System.Collections.Generic;
using NiL.Exev;

namespace NiL.Hub
{
    public sealed class RemoteHub
    {
        public string Name { get; internal set; }
        public long Id { get; internal set; }
        public IEnumerable<string> Interfaces => _interfaces;

        internal readonly ConnectionsContainer _connections = new ConnectionsContainer();
        internal readonly HashSet<string> _interfaces = new HashSet<string>();
        internal readonly TypesMapLayer _typesMap;
        internal readonly ExpressionSerializer _expressionSerializer;

        public RemoteHub(TypesMapLayer typesMap)
        {
            _typesMap = typesMap ?? throw new System.ArgumentNullException(nameof(typesMap));
            _expressionSerializer = new ExpressionSerializer(_typesMap);
        }
    }
}
