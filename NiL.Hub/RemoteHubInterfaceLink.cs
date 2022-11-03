namespace NiL.Hub
{
    internal struct RemoteHubInterfaceLink
    {
        public readonly RemoteHub Hub;
        public readonly uint InterfaceId;
        public readonly int ShareId;

        public RemoteHubInterfaceLink(RemoteHub hub, uint interfaceId, int shareId)
        {
            Hub = hub;
            InterfaceId = interfaceId;
            ShareId = shareId;
        }
    }
}
