using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Actor.Serializable;

namespace Actor.Net
{

    public class Message : EventData<short>
    {
        public const short SYS_NO_TYPE_MSG = -1;

	    // See SysMsgType for deals 
        /// Event.type has two meaning: <0 mean systemMessage, >0 means logic message
        ///     
	    /// public short type;
		
	    public Message(short type)
	    {
		    this.type = type;
	    }	
	
	    public Message()
	    {
            this.type = SYS_NO_TYPE_MSG;
	    }

        public static T ToConcreteMsg<T>(Message message) where T : Message
	    {
		    return (T)message;	
	    }		
    }

    public class NetMessage : Message
    {
        public static Exception CANCELED = new Exception("CANCELED");
        public static Exception INVALID_HEADER_TYPE = new Exception("INVALID_HEADER_TYPE");

	    public const int LEN_SIZE = 2;
	    public const int TYPE_SIZE = 2;
	    public const int DATA_TYPE_SIZE = 1;

        public short GetHeaderType() { return _headerType; }
        /// The real stored is byte(uint8)
        protected short _headerType;
	    protected ISerializableData _data; 

        public NetMessage() :base() { }
		
	    public NetMessage(short netMsgType, ISerializableData data) : base()
	    {
		    this.type = netMsgType;
		    _data = data;
	    }
		
	    public T GetData<T>()
	    {
		    return (T)_data;
	    }
	       
        override public String ToString() 
        {
            return _data.ToString();
        }    	 	
    
	    public ByteBuffer Serialize()
	    {
		    return Serialize(_bufferCreator);
	    }	    
    
	    public ByteBuffer Serialize(Func<int, ByteBuffer> bufferCreator)
	    {
            int headerSize = HeaderType.GetHeaderSize(_headerType);
            ByteBuffer byteBuffer = _data.Serialize(LEN_SIZE + headerSize, bufferCreator);

            int length = byteBuffer.position;
            byteBuffer.position = 0;
            byteBuffer.WriteUShort((ushort)length);

            _serializeHeader(byteBuffer);

            byteBuffer.position = length;

            return byteBuffer;
	    }

        public void Deserialize(ByteBuffer byteBuffer)
	    {
            /// length field is useless here, just skip it.
            byteBuffer.position = byteBuffer.position + NetMessage.LEN_SIZE;

            short dataType = _deserializeHeader(byteBuffer);

            switch (dataType)
            {
                case _DataTypes.EMPTY_OBJECT:
                    _data = new EmptyObject();
                    break;

                case _DataTypes.SIMPLE_ARRAY:
                    _data = new SimpleArray();
                    break;

                case _DataTypes.SIMPLE_OBJECT:
                    _data = new SimpleObject();
                    break;

                case _DataTypes.POJO_OBJECT:
                    _data = new PojoObject();
                    break;
            }

            _data.Deserialize(byteBuffer);          
	    }

        ///---
        /// Override it to customize message header
        virtual protected void _serializeHeader(ByteBuffer byteBuffer)
        {
            byteBuffer.WriteByte((byte)_headerType);
            byteBuffer.WriteShort(this.type);
            byteBuffer.WriteByte((byte)_data.GetFormat());
        }

        /// Override it to customize message header
        virtual protected short _deserializeHeader(ByteBuffer byteBuffer)
        {
            _headerType = byteBuffer.FReadByte();
            this.type = byteBuffer.FReadShort();
            short dataType = byteBuffer.FReadByte();
            return dataType;
        }

        private ByteBuffer _bufferCreator(int size)
        {
            return new ByteBuffer(size);
        }

    }

}


