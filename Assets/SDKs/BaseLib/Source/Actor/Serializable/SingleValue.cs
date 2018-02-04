using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Actor.Serializable
{
    public class SingleValue : ISerializableData
    {
        protected Object _value;
        protected byte _type;

        public SingleValue(int value)
        {
            this._value = value;
            this._type = _DataTypes.INT;
        }

        public SingleValue(bool value)
        {
            this._value = value;
            this._type = _DataTypes.BOOL;
        }

        public SingleValue(float value)
        {
            this._value = value;
            this._type = _DataTypes.FLOAT;
        }

        public T GetValue<T>()
        {
            return (T)(_value);
        }

        public SingleValue(String value)
        {

            _value = Encoding.UTF8.GetBytes(value);
            this._type = _DataTypes.STRING;
        }

        public ByteBuffer Serialize(int reservedSize, Func<int, ByteBuffer> bufferCreator)
        {
            int totalLen = reservedSize;

            if ((_value == null))
            {
                throw new Exception("Null data found in the SingleValue!");
            }

            if (_type == _DataTypes.INT)
            {
                totalLen += 4;
            }
            else if (_type == _DataTypes.STRING)
            {
                totalLen = (totalLen + (((byte[])(_value)).Length + 2));
            }
            else if ((this._type == _DataTypes.BOOL))
            {
                totalLen++;
            }
            else
            {
                throw new Exception("Unsupported type!");
            }

            totalLen++;
            ///  Single value, so only one byte needed to store the byte.
            if ((totalLen > 65535))
            {
                throw new Exception("Data exceed max size!");
            }

            ByteBuffer buffer = bufferCreator(totalLen);
            buffer.position = buffer.position + reservedSize;

            if ((this._type == _DataTypes.INT))
            {
                buffer.WriteByte(_DataTypes.INT);
                buffer.WriteInt(((int)(_value)));
            }
            else if ((this._type == _DataTypes.STRING))
            {
                buffer.WriteByte(_DataTypes.STRING);
                byte[] byteArray = ((byte[])(this._value));
                buffer.WriteUShort((ushort)byteArray.Length);
                buffer.WriteBytes(byteArray);
            }
            else if ((this._type == _DataTypes.BOOL))
            {
                buffer.WriteByte(_DataTypes.BOOL);
                bool b = ((bool)(_value));
                buffer.WriteByte((byte)(b ? 1 : 0));
            }
            else
            {
                throw new Exception("Unsupported type!");
            }

            return buffer;
        }

        public void Deserialize(ByteBuffer buffer)
        {
            byte type = buffer.FReadByte();
            if ((type == _DataTypes.INT))
            {
                _value = buffer.FReadInt();
            }
            else if ((type == _DataTypes.STRING))
            {
                int strLen = buffer.FReadUShort();
                _value = buffer.FReadString(strLen);
            }
            else if ((type == _DataTypes.BOOL))
            {
                _value = buffer.FReadByte() == 1 ? true : false;
            }
            else
            {
                throw new Exception("Corrupted data!");
            }

        }

        public byte GetFormat()
        {
            return _DataTypes.SINGLE_VALUE;
        }

        override public String ToString()
        {
            return ("Value: " + this._value);
        }
    }

}