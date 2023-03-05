using System;

namespace NiL.Hub;

public sealed class RemoteHubRegisteredEventArgs : EventArgs
{
    public RemoteHub RemoteHub { get; init; }
}
