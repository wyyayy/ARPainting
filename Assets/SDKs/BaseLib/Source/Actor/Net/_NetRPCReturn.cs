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
	    2. header = headerType(1) + rpcID(2) + dataType(1)
    */
    public class _NetRPCReturn : NetMessage
    {
        public const short SYS_NET_RPC_RET = -5;

	    public ushort _GetRPCID() { return _rpcID; }
	    public void _SetRPCID(ushort value) { _rpcID = value; }
	    /// Note: the stored value is ushort.
	    protected ushort _rpcID;
	
	    public _NetRPCReturn() : base()
	    {
            this.type = SYS_NET_RPC_RET;
	    }

        public _NetRPCReturn(short rpcType, ushort rpcID, ISerializableData data) : base()
	    {
		    _headerType = HeaderType.RPCReturn;
		
		    _rpcID = rpcID;
		    _data = data;		
		 
		    this.type = rpcType;
	    }	
		
	    override protected void _serializeHeader(ByteBuffer byteBuffer)
	    {
            byteBuffer.WriteByte((byte)_headerType);
            byteBuffer.WriteUShort((ushort)_rpcID);
            byteBuffer.WriteByte(_data.GetFormat());
	    }
	
	    override protected short _deserializeHeader(ByteBuffer byteBuffer)
	    {
            _headerType = byteBuffer.FReadByte();
            Debugger.Assert(_headerType == HeaderType.RPCReturn);
            _rpcID = byteBuffer.FReadUShort();
            short dataType = byteBuffer.FReadByte();
		    return dataType;
	    }	
    }

}


