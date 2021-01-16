namespace NiL.Hub
{
    internal struct RemoteHubInterfaceLink
    {
        public readonly RemoteHub Hub;
        public readonly uint InterfaceId;

        public RemoteHubInterfaceLink(RemoteHub hub, uint interfaceId)
        {
            Hub = hub;
            InterfaceId = interfaceId;
        }
    }
}
