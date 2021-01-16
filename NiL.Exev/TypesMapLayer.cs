using System;
using System.Collections;
using System.Collections.Generic;

namespace NiL.Exev
{
    public sealed class TypesMapLayer : IEnumerable<KeyValuePair<uint, Type>>
    {
        private readonly Dictionary<Type, uint> _typeToId = new Dictionary<Type, uint>();
        private readonly Dictionary<uint, Type> _idToType = new Dictionary<uint, Type>();
        private readonly TypesMapLayer _parentMap;

        public TypesMapLayer()
        {
        }

        public TypesMapLayer(TypesMapLayer parentMap)
        {
            _parentMap = parentMap ?? throw new ArgumentNullException(nameof(parentMap));
        }

        public int Count => _typeToId.Count + (_parentMap != null ? _parentMap.Count : 0);

        public bool TryGetId(Type type, out uint id)
        {
            return _typeToId.TryGetValue(type, out id) || (_parentMap != null && _parentMap.TryGetId(type, out id));
        }

        public bool TryGetType(uint id, out Type type)
        {
            return _idToType.TryGetValue(id, out type) || (_parentMap != null && _parentMap.TryGetType(id, out type));
        }

        public bool HasOwn(Type type)
        {
            return _typeToId.ContainsKey(type);
        }

        public uint GetId(Type type)
        {
            if (!TryGetId(type, out var id))
                throw new ArgumentException("Type " + type + " is not registered");

            return id;
        }

        public Type GetType(uint id)
        {
            if (!TryGetType(id, out var type))
                throw new ArgumentException("Id " + id + " is not registered");

            return type;
        }

        public void Add(Type type, uint id)
        {
            if (_typeToId.ContainsKey(type))
                throw new ArgumentException("Type " + type + " already added to collection");

            if (_idToType.ContainsKey(id))
                throw new ArgumentException("Id " + id + " already added to collection");

            _idToType.Add(id, type);

            try
            {
                _typeToId.Add(type, id);
            }
            catch
            {
                _idToType.Remove(id);
                throw;
            }
        }

        public IEnumerator<KeyValuePair<uint, Type>> GetEnumerator()
        {
            return _idToType.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
