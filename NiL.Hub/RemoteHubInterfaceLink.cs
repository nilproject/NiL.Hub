namespace NiL.Hub
{
    internal struct RemoteHubInterfaceLink
    {
        public readonly RemoteHub Hub;
        public readonly uint InterfaceId;
        public readonly int Version;

        public RemoteHubInterfaceLink(RemoteHub hub, uint interfaceId, int version)
        {
            Hub = hub;
            InterfaceId = interfaceId;
            Version = version;
        }
    }
}
