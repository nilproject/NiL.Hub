using System;

namespace NiL.Hub
{
    [AttributeUsage(
        AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class DenyRemoteCallAttribute : Attribute
    {
    }
}
