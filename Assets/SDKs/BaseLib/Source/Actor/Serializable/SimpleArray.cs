using System;
using System.Collections.Generic;
using System.Text;

namespace Actor.Serializable
{

    public class SimpleArray : ISerializableData
    {
        protected List<Object> _datas;

        public SimpleArray() { }

        public SimpleArray(int count)
        {
            _datas = new List<Object>();
            for (int i = 0; i < count; ++i) _datas.Add(null);
        }

        public void SetString(int index, String value)
        {
            _datas[index] = Encoding.UTF8.GetBytes(value);
        }

        public void SetInt(int index, int value)
        {
            _datas[index] = value;
        }

        public void SetBool(int index, int value)
        {
            _datas[index] = value;
        }

        ///---
        public int GetInt(int index)
        {
            return (int)_datas[index];
        }

        public String GetString(int index)
        {
            return (String)_datas[index];
        }

        public bool GetBool(int index)
        {
            return (bool)_datas[index];
        }

        public ByteBuffer Serialize(int reservedSize, Func<int, ByteBuffer> bufferCreator)
        {
            /// Calculate total length count
            int totalLen = reservedSize;

            foreach (var obj in _datas)
            {
                if (obj == null) throw new Exception("Null data found in the SimpleArray!");

                if (obj is int)
                {
                    totalLen += 4;
                }
                else if (obj is byte[])
                {
                    totalLen += ((byte[])obj).Length + 2;
                }
                else if (obj is bool)
                {
                    totalLen += 1;
                }
                else throw new Exception("Unsupported type!");
            }

            totalLen += _datas.Count;
            if (totalLen > 65535) throw new Exception("Data exceed max size!");

            /// Write to buffer
            ByteBuffer buffer = bufferCreator(totalLen);
            buffer.WriteUShort((ushort)totalLen);

            buffer.position = buffer.position + reservedSize;

            foreach (var obj in _datas)
            {
                if (obj is int)
                {
                    buffer.WriteByte(_DataTypes.INT);
                    buffer.WriteInt((int)obj);
                }
                else if (obj is byte[])
                {
                    buffer.WriteByte(_DataTypes.STRING);
                    byte[] byteArray = (byte[])obj;
                    buffer.WriteUShort((ushort)byteArray.Length);
                    buffer.WriteBytes(byteArray);
                }
                else if (obj is bool)
                {
                    buffer.WriteByte(_DataTypes.BOOL);
                    bool b = (bool)obj;
                    buffer.WriteByte((byte)(b ? 1 : 0));
                }
                else
                {
                    throw new Exception("Unsupported type!");
                }
            }

            return buffer;
        }

        public void Deserialize(ByteBuffer buffer)
        {
            _datas = new List<Object>();

            while (buffer.HashRemaining(true))
            {
                byte type = buffer.FReadByte();

                if (type == _DataTypes.INT)
                {
                    _datas.Add(buffer.FReadInt());
                }
                else if (type == _DataTypes.STRING)
                {
                    int strLen = buffer.FReadUShort();

                    string str = buffer.FReadString(strLen);
                    _datas.Add(str);
                }
                else if (type == _DataTypes.BOOL)
                {
                    _datas.Add(buffer.FReadByte() == 1 ? true : false);
                }
                else
                {
                    throw new Exception("Corrupted data!");
                }
            }
        }

        override public String ToString()
        {
            if (_datas == null) return "Empty";

            StringBuilder builder = new StringBuilder();

            builder.Append("[");

            int count = _datas.Count;
            int i = 0;

            foreach (var item in _datas)
            {
                builder.Append(item);
                i++;

                if (i != count) builder.Append(", ");
            }

            builder.Append("]");

            return builder.ToString();
        }

        public byte GetFormat()
        {
            return _DataTypes.SIMPLE_ARRAY;
        }

    }
}