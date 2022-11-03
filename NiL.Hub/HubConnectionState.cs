namespace NiL.Hub
{
    public enum HubConnectionState
    {
        NotInitialized,
        HelloSent,
        HelloResponseReceived,
        Active,
        Disconnecting,
        Disconnected,
        Disposed,
    }
}
