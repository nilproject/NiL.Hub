using System;
using System.Collections.Generic;

namespace NiL.Hub
{
    internal class SharedInterface : ISharedInterface
    {
        public readonly List<RemoteHubInterfaceLink> Hubs;
        public object LocalImplementation;

        public string Name { get; }

        public SharedInterface(string fullName)
        {
            Hubs = new List<RemoteHubInterfaceLink>();
            Name = fullName;
        }

        protected SharedInterface(string fullName, List<RemoteHubInterfaceLink> remoteHubs)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException($"\"{nameof(fullName)}\" cannot be empty of white space", nameof(fullName));

            Hubs = remoteHubs ?? throw new ArgumentNullException(nameof(remoteHubs));
            Name = fullName;
        }
    }
}
