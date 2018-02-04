using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Actor.Serializable
{
    public class EmptyObject : ISerializableData
    {
        public ByteBuffer Serialize(int reservedSize, Func<int, ByteBuffer> bufferCreator)
        {
            ByteBuffer buffer = bufferCreator(reservedSize);
            buffer.position = reservedSize;
            return buffer;
        }

        public void Deserialize(ByteBuffer buffer)
        {
        }

        public byte GetFormat()
        {
            // TODO Auto-generated method stub
            return _DataTypes.EMPTY_OBJECT;
        }

        override public String ToString()
        {
            return "EmptyObject";
        }
    }

}