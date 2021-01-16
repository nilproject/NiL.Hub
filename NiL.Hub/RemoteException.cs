using System;
using System.Collections.Generic;
using System.Text;

namespace NiL.Hub
{
    public sealed class RemoteException : Exception
    {
        public RemoteException(string message) : base(message)
        {
        }
    }
}
