using System;

namespace NiL.Exev
{
    public sealed partial class ExpressionEvaluator
    {            
        private static object mul_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left * (long)right;
                    case TypeCode.UInt64: return (ulong)left * (ulong)right;
                    case TypeCode.Int32: return (int)left * (int)right;
                    case TypeCode.UInt32: return (uint)left * (uint)right;
                    case TypeCode.Int16: return (short)left * (short)right;
                    case TypeCode.UInt16: return (ushort)left * (ushort)right;
                    case TypeCode.Byte: return (byte)left * (byte)right;
                    case TypeCode.SByte: return (sbyte)left * (sbyte)right;

                    default: throw new NotImplementedException("Mul for " + type);
                }
            }
        }
                
        private static object add_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left + (long)right;
                    case TypeCode.UInt64: return (ulong)left + (ulong)right;
                    case TypeCode.Int32: return (int)left + (int)right;
                    case TypeCode.UInt32: return (uint)left + (uint)right;
                    case TypeCode.Int16: return (short)left + (short)right;
                    case TypeCode.UInt16: return (ushort)left + (ushort)right;
                    case TypeCode.Byte: return (byte)left + (byte)right;
                    case TypeCode.SByte: return (sbyte)left + (sbyte)right;

                    default: throw new NotImplementedException("Add for " + type);
                }
            }
        }
                
        private static object sub_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left - (long)right;
                    case TypeCode.UInt64: return (ulong)left - (ulong)right;
                    case TypeCode.Int32: return (int)left - (int)right;
                    case TypeCode.UInt32: return (uint)left - (uint)right;
                    case TypeCode.Int16: return (short)left - (short)right;
                    case TypeCode.UInt16: return (ushort)left - (ushort)right;
                    case TypeCode.Byte: return (byte)left - (byte)right;
                    case TypeCode.SByte: return (sbyte)left - (sbyte)right;

                    default: throw new NotImplementedException("Sub for " + type);
                }
            }
        }
                
        private static object div_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left / (long)right;
                    case TypeCode.UInt64: return (ulong)left / (ulong)right;
                    case TypeCode.Int32: return (int)left / (int)right;
                    case TypeCode.UInt32: return (uint)left / (uint)right;
                    case TypeCode.Int16: return (short)left / (short)right;
                    case TypeCode.UInt16: return (ushort)left / (ushort)right;
                    case TypeCode.Byte: return (byte)left / (byte)right;
                    case TypeCode.SByte: return (sbyte)left / (sbyte)right;

                    default: throw new NotImplementedException("Div for " + type);
                }
            }
        }
                
        private static object mod_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left % (long)right;
                    case TypeCode.UInt64: return (ulong)left % (ulong)right;
                    case TypeCode.Int32: return (int)left % (int)right;
                    case TypeCode.UInt32: return (uint)left % (uint)right;
                    case TypeCode.Int16: return (short)left % (short)right;
                    case TypeCode.UInt16: return (ushort)left % (ushort)right;
                    case TypeCode.Byte: return (byte)left % (byte)right;
                    case TypeCode.SByte: return (sbyte)left % (sbyte)right;

                    default: throw new NotImplementedException("Mod for " + type);
                }
            }
        }
                
        private static object and_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left & (long)right;
                    case TypeCode.UInt64: return (ulong)left & (ulong)right;
                    case TypeCode.Int32: return (int)left & (int)right;
                    case TypeCode.UInt32: return (uint)left & (uint)right;
                    case TypeCode.Int16: return (short)left & (short)right;
                    case TypeCode.UInt16: return (ushort)left & (ushort)right;
                    case TypeCode.Byte: return (byte)left & (byte)right;
                    case TypeCode.SByte: return (sbyte)left & (sbyte)right;

                    default: throw new NotImplementedException("And for " + type);
                }
            }
        }
                
        private static object or_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left | (long)right;
                    case TypeCode.UInt64: return (ulong)left | (ulong)right;
                    case TypeCode.Int32: return (int)left | (int)right;
                    case TypeCode.UInt32: return (uint)left | (uint)right;
                    case TypeCode.Int16: return (short)left | (short)right;
                    case TypeCode.UInt16: return (ushort)left | (ushort)right;
                    case TypeCode.Byte: return (byte)left | (byte)right;
                    case TypeCode.SByte: return (sbyte)left | (sbyte)right;

                    default: throw new NotImplementedException("Or for " + type);
                }
            }
        }
                
        private static object xor_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left ^ (long)right;
                    case TypeCode.UInt64: return (ulong)left ^ (ulong)right;
                    case TypeCode.Int32: return (int)left ^ (int)right;
                    case TypeCode.UInt32: return (uint)left ^ (uint)right;
                    case TypeCode.Int16: return (short)left ^ (short)right;
                    case TypeCode.UInt16: return (ushort)left ^ (ushort)right;
                    case TypeCode.Byte: return (byte)left ^ (byte)right;
                    case TypeCode.SByte: return (sbyte)left ^ (sbyte)right;

                    default: throw new NotImplementedException("Xor for " + type);
                }
            }
        }
                
        private static object mul_checked(object left, object right, Type type)
        {
            checked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left * (long)right;
                    case TypeCode.UInt64: return (ulong)left * (ulong)right;
                    case TypeCode.Int32: return (int)left * (int)right;
                    case TypeCode.UInt32: return (uint)left * (uint)right;
                    case TypeCode.Int16: return (short)left * (short)right;
                    case TypeCode.UInt16: return (ushort)left * (ushort)right;
                    case TypeCode.Byte: return (byte)left * (byte)right;
                    case TypeCode.SByte: return (sbyte)left * (sbyte)right;

                    default: throw new NotImplementedException("Mul for " + type);
                }
            }
        }
                
        private static object add_checked(object left, object right, Type type)
        {
            checked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left + (long)right;
                    case TypeCode.UInt64: return (ulong)left + (ulong)right;
                    case TypeCode.Int32: return (int)left + (int)right;
                    case TypeCode.UInt32: return (uint)left + (uint)right;
                    case TypeCode.Int16: return (short)left + (short)right;
                    case TypeCode.UInt16: return (ushort)left + (ushort)right;
                    case TypeCode.Byte: return (byte)left + (byte)right;
                    case TypeCode.SByte: return (sbyte)left + (sbyte)right;

                    default: throw new NotImplementedException("Add for " + type);
                }
            }
        }
                
        private static object sub_checked(object left, object right, Type type)
        {
            checked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left - (long)right;
                    case TypeCode.UInt64: return (ulong)left - (ulong)right;
                    case TypeCode.Int32: return (int)left - (int)right;
                    case TypeCode.UInt32: return (uint)left - (uint)right;
                    case TypeCode.Int16: return (short)left - (short)right;
                    case TypeCode.UInt16: return (ushort)left - (ushort)right;
                    case TypeCode.Byte: return (byte)left - (byte)right;
                    case TypeCode.SByte: return (sbyte)left - (sbyte)right;

                    default: throw new NotImplementedException("Sub for " + type);
                }
            }
        }
                
        private static object equal_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left == (long)right;
                    case TypeCode.UInt64: return (ulong)left == (ulong)right;
                    case TypeCode.Int32: return (int)left == (int)right;
                    case TypeCode.UInt32: return (uint)left == (uint)right;
                    case TypeCode.Int16: return (short)left == (short)right;
                    case TypeCode.UInt16: return (ushort)left == (ushort)right;
                    case TypeCode.Byte: return (byte)left == (byte)right;
                    case TypeCode.SByte: return (sbyte)left == (sbyte)right;

                    default: throw new NotImplementedException("Equal for " + type);
                }
            }
        }
                
        private static object notEqual_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left != (long)right;
                    case TypeCode.UInt64: return (ulong)left != (ulong)right;
                    case TypeCode.Int32: return (int)left != (int)right;
                    case TypeCode.UInt32: return (uint)left != (uint)right;
                    case TypeCode.Int16: return (short)left != (short)right;
                    case TypeCode.UInt16: return (ushort)left != (ushort)right;
                    case TypeCode.Byte: return (byte)left != (byte)right;
                    case TypeCode.SByte: return (sbyte)left != (sbyte)right;

                    default: throw new NotImplementedException("NotEqual for " + type);
                }
            }
        }
                
        private static object more_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left > (long)right;
                    case TypeCode.UInt64: return (ulong)left > (ulong)right;
                    case TypeCode.Int32: return (int)left > (int)right;
                    case TypeCode.UInt32: return (uint)left > (uint)right;
                    case TypeCode.Int16: return (short)left > (short)right;
                    case TypeCode.UInt16: return (ushort)left > (ushort)right;
                    case TypeCode.Byte: return (byte)left > (byte)right;
                    case TypeCode.SByte: return (sbyte)left > (sbyte)right;

                    default: throw new NotImplementedException("More for " + type);
                }
            }
        }
                
        private static object less_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left < (long)right;
                    case TypeCode.UInt64: return (ulong)left < (ulong)right;
                    case TypeCode.Int32: return (int)left < (int)right;
                    case TypeCode.UInt32: return (uint)left < (uint)right;
                    case TypeCode.Int16: return (short)left < (short)right;
                    case TypeCode.UInt16: return (ushort)left < (ushort)right;
                    case TypeCode.Byte: return (byte)left < (byte)right;
                    case TypeCode.SByte: return (sbyte)left < (sbyte)right;

                    default: throw new NotImplementedException("Less for " + type);
                }
            }
        }
                
        private static object moreOrEqual_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left >= (long)right;
                    case TypeCode.UInt64: return (ulong)left >= (ulong)right;
                    case TypeCode.Int32: return (int)left >= (int)right;
                    case TypeCode.UInt32: return (uint)left >= (uint)right;
                    case TypeCode.Int16: return (short)left >= (short)right;
                    case TypeCode.UInt16: return (ushort)left >= (ushort)right;
                    case TypeCode.Byte: return (byte)left >= (byte)right;
                    case TypeCode.SByte: return (sbyte)left >= (sbyte)right;

                    default: throw new NotImplementedException("MoreOrEqual for " + type);
                }
            }
        }
                
        private static object lessOrEqual_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left <= (long)right;
                    case TypeCode.UInt64: return (ulong)left <= (ulong)right;
                    case TypeCode.Int32: return (int)left <= (int)right;
                    case TypeCode.UInt32: return (uint)left <= (uint)right;
                    case TypeCode.Int16: return (short)left <= (short)right;
                    case TypeCode.UInt16: return (ushort)left <= (ushort)right;
                    case TypeCode.Byte: return (byte)left <= (byte)right;
                    case TypeCode.SByte: return (sbyte)left <= (sbyte)right;

                    default: throw new NotImplementedException("LessOrEqual for " + type);
                }
            }
        }
                
        private static object rightShift_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left >> (int)right;
                    case TypeCode.UInt64: return (ulong)left >> (int)right;
                    case TypeCode.Int32: return (int)left >> (int)right;
                    case TypeCode.UInt32: return (uint)left >> (int)right;
                    case TypeCode.Int16: return (short)left >> (int)right;
                    case TypeCode.UInt16: return (ushort)left >> (int)right;
                    case TypeCode.Byte: return (byte)left >> (int)right;
                    case TypeCode.SByte: return (sbyte)left >> (int)right;

                    default: throw new NotImplementedException("RightShift for " + type);
                }
            }
        }
                
        private static object leftShift_unchecked(object left, object right, Type type)
        {
            unchecked
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int64: return (long)left << (int)right;
                    case TypeCode.UInt64: return (ulong)left << (int)right;
                    case TypeCode.Int32: return (int)left << (int)right;
                    case TypeCode.UInt32: return (uint)left << (int)right;
                    case TypeCode.Int16: return (short)left << (int)right;
                    case TypeCode.UInt16: return (ushort)left << (int)right;
                    case TypeCode.Byte: return (byte)left << (int)right;
                    case TypeCode.SByte: return (sbyte)left << (int)right;

                    default: throw new NotImplementedException("LeftShift for " + type);
                }
            }
        }
        }
}