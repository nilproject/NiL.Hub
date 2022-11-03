using System;

namespace NiL.Hub
{
    public sealed class ShareNotFoundException : Exception
    {
        public ShareNotFoundException(Hub localHub, Type @interface, int shareId, string message = null)
            : base(message ?? "Share #" + shareId + " for interface \"" + @interface.FullName + "\" not found")
        {
            LocalHub = localHub;
            ShareId = shareId;
        }

        public Hub LocalHub { get; }
        public int ShareId { get; }
    }
}
