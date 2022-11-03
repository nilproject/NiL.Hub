using System;
using System.Collections.Generic;

namespace NiL.Hub
{
    internal class SharedInterface : ISharedInterface
    {
        public readonly List<RemoteHubInterfaceLink> HubLinks;
        public object LocalImplementation;
        public uint LocalId;
        public int LocalShareId;

        public string Name { get; }

        public SharedInterface(string fullName)
        {
            HubLinks = new List<RemoteHubInterfaceLink>();
            Name = fullName;
        }

        protected SharedInterface(string fullName, List<RemoteHubInterfaceLink> remoteHubs)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException($"\"{nameof(fullName)}\" cannot be empty of white space", nameof(fullName));

            HubLinks = remoteHubs ?? throw new ArgumentNullException(nameof(remoteHubs));
            Name = fullName;
        }
    }
}
