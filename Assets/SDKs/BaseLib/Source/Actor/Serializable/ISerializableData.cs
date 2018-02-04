using System;

namespace Actor.Serializable
{
    public class _DataTypes
    {
        public const byte EMPTY_OBJECT = 1;	
        public const byte SIMPLE_ARRAY = 2;
        public const byte SIMPLE_OBJECT = 3;
        public const byte POJO_OBJECT = 4;
        public const byte SINGLE_VALUE = 5; 
	
	    public const byte INT = 1;
	    public const byte BOOL = 2;
	    public const byte STRING = 3;
	    public const byte FLOAT = 4;	
	    public const byte BINARY = 5;
    }


    public interface ISerializableData
    {	
	    /// reservedSize: reserve some space at the buffer head (after the totalLen). 
	    /// For example, reserve two byte to store the message's type.
	    ByteBuffer Serialize(int reservedSize, System.Func<int, ByteBuffer> bufferCreator);
        void Deserialize(ByteBuffer buffer);

        byte GetFormat(); 
    }

}