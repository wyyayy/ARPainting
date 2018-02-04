using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Actor.Serializable;

using BaseLib;

namespace Actor.Net
{
    /* 
    Binary format: length + header + data (variant length)

    Note:
        1. length is the entire size of a message.	
        2. header = headerType(1) + rpcID(2) + type(2) + dataType(1)
            For NetRPCCall, the headerType is 
    */
    public class NetRPCCall : NetMessage
    {
        public void _SetRPCID(int rpcID) { Debugger.Assert(rpcID > 0 && rpcID <= 65535); _rpcID = (ushort)rpcID; }
        public ushort _GetRPCID() { return _rpcID; }
        /// Note: the store value is a unsigned short!
        protected ushort _rpcID;

        public NetRPCCall()
            : base()
        {
            _headerType = HeaderType.RPCCall;
        }

        public NetRPCCall(short rpcType, ISerializableData data)
            : base(rpcType, data)
        {
            _headerType = HeaderType.RPCCall;
        }

        override protected void _serializeHeader(ByteBuffer byteBuffer)
        {
            byteBuffer.WriteByte((byte)_headerType);
            byteBuffer.WriteUShort(_rpcID);
            byteBuffer.WriteShort(this.type);
            byteBuffer.WriteByte(_data.GetFormat());
        }

        override protected short _deserializeHeader(ByteBuffer byteBuffer)
        {
            _headerType = byteBuffer.FReadByte();
            Debugger.Assert(_headerType == HeaderType.RPCCall);

            _rpcID = byteBuffer.FReadUShort();

            this.type = byteBuffer.FReadShort();
            short dataType = byteBuffer.FReadByte();
            return dataType;
        }
    }

}


