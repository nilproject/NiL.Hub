using System;
using System.Collections.Generic;
using System.Text;

namespace NiL.Exev
{
    internal static class AdditionalTypeCodes
    {
        internal const TypeCode _ArrayTypeCode = (TypeCode)0xfe;
        internal const TypeCode _BoxiedTypeCode = (TypeCode)0xfc;
        internal const TypeCode _RegisteredTypeCode = (TypeCode)0xfd;
        internal const TypeCode _UnregisteredTypeCode = (TypeCode)0xfa;
    }
}
