namespace NiL.Hub
{
    internal enum PackageType : byte
    {
        /// <summary>
        /// Int32: Random
        /// </summary>
        Ping = 0x00,

        /// <summary>
        /// Int32: Random
        /// </summary>
        Pong = 0x01,

        /// <summary>
        /// Int64: Receiver hub id <br/>
        /// Int64: Sender hub id <br/>
        /// UInt16: Bytes count = N <br/>
        /// N of Byte: Nested package
        /// </summary>
        RetransmitTo = 0x11,

        /// <summary>
        /// String: name in UTF8 <br/>
        /// Byte: Hubs count = N <br/>
        /// N of 
        ///     Int64: HubIds
        ///     UInt32: InterfaceId
        /// </summary>
        RegisterInterface = 0x12,

        /// <summary>
        /// String: Interface name <br/>
        /// Int64: HubId
        /// </summary>
        UnRegisterInterface = 0x13,

        /// <summary>
        /// Int64: Hub id <br/>
        /// String: Hub name <br/>
        /// Int32: Distance
        /// </summary>
        HubIsAvailable = 0x14,

        /// <summary>
        /// Int64: Hub id
        /// </summary>
        HubIsUnavailable = 0x15,

        /// <summary>
        /// none
        /// </summary>
        Disconnect = 0x16,

        /// <summary>
        /// none
        /// </summary>
        ReadyForDisconnect = 0x17,

        /// <summary>
        /// Int64: Hub id <br/>
        /// String: Hub name
        /// </summary>
        Hello = 0x18,

        /// <summary>
        /// Int64: Hub id <br/>
        /// String: Hub name
        /// </summary>
        HelloResponse = 0x19,

        /// <summary>
        /// Int32: Result await id <br/>
        /// Int16: Size of serialized code = N <br/>
        /// N of Byte: Serialized code
        /// </summary>
        Call = 0x20,

        /// <summary>
        /// Int32: Result await id <br/>
        /// Int16: Size of serialized value = N <br/>
        /// N of Byte: Serialized value
        /// </summary>
        Result = 0x21,

        /// <summary>
        /// Int32: Result await id <br/>
        /// String: Message
        /// </summary>
        Exception = 0x22,

        /// <summary>
        /// Int32: Error code <br/>
        /// String: Message
        /// </summary>
        Error = 0xff,
    }
}
