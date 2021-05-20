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
        /// Int32: Stream id
        /// </summary>
        StreamGetInfo = 0x30,

        /// <summary>
        /// Int32: Stream id
        /// Int64: length <br/>
        /// Int64: position <br/>
        /// byte: flags <br/>
        ///       0x1: can read <br/>
        ///       0x2: can write <br/>
        ///       0x4: can seek
        /// </summary>
        StreamInfo = 0x31,

        /// <summary>
        /// Int32: Stream id
        /// </summary>
        StreamClose = 0x32,

        /// <summary>
        /// Int32: Stream id <br/>
        /// Int16: Bytes count
        /// </summary>
        StreamRead = 0x33,

        /// <summary>
        /// Int32: Stream id <br/>
        /// Int16: Bytes count <br/>
        /// N of byte: data
        /// </summary>
        StreamWrite = 0x34,

        /// <summary>
        /// Int32: Stream id <br/>
        /// Int64: new position
        /// </summary>
        StreamSeek = 0x35,

        /// <summary>
        /// Int32: Stream id <br/>
        /// Int16: bytes count <br/>
        /// N of bytes
        /// </summary>
        StreamData = 0x36,

        /// <summary>
        /// Int32: Error code <br/>
        /// String: Message
        /// </summary>
        Error = 0xff,
    }
}
