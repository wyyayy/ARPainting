using System;
using System.Collections.Generic;
using System.Text;

using BaseLib;

using JsonFx.Json;

namespace Actor.Serializable
{

    /// Key-value paired net data container.
    public class SimpleObject : ISerializableData
    {
        protected Dictionary<byte[], Object> _writeDatas;
        protected Dictionary<String, Object> _readDatas;

        public SimpleObject() { }

        public void SetString(String key, String value)
        {
            if (_writeDatas == null) _writeDatas = new Dictionary<byte[], Object>();
            else Debugger.Assert(_readDatas == null);

            Debugger.Assert(key != null);
            try
            {
                _writeDatas.Add(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
            }
            catch (Exception e) { Debugger.LogError(e.ToString()); }
        }

        public void SetInt(String key, int value)
        {
            if (_writeDatas == null) _writeDatas = new Dictionary<byte[], Object>();
            else Debugger.Assert(_readDatas == null);

            Debugger.Assert(key != null);
            try
            {
                _writeDatas.Add(Encoding.UTF8.GetBytes(key), value);
            }
            catch (Exception e) { Debugger.LogError(e.ToString()); }
        }

        public void SetBool(String key, bool value)
        {
            if (_writeDatas == null) _writeDatas = new Dictionary<byte[], Object>();
            else Debugger.Assert(_readDatas == null);

            Debugger.Assert(key != null);
            try
            {
                _writeDatas.Add(Encoding.UTF8.GetBytes(key), value);
            }
            catch (Exception e) { Debugger.LogError(e.ToString()); }
        }

        public int GetInt(String key)
        {
            return (int)_readDatas[key];
        }

        public String GetString(String key)
        {
            return (String)_readDatas[key];
        }

        public bool GetBool(String key)
        {
            return (bool)_readDatas[key];
        }

        public ByteBuffer Serialize(int reservedSize, Func<int, ByteBuffer> bufferCreator)
        {
            /// Calculate total length count
            int totalLen = reservedSize;

            foreach (var entry in _writeDatas)
            {
                byte[] key = entry.Key;
                Object obj = entry.Value;

                if (obj == null) throw new Exception("Null data found in the SimpleObject!");
                Debugger.Assert(key != null);

                /// Null terminated string
                totalLen += key.Length + 1;

                if (obj is int)
                {
                    totalLen += 4;
                }
                else if (obj is byte[])
                {
                    totalLen += 2; /// Use two byte (ushort) store string length. 
                    totalLen += ((byte[])obj).Length;
                }
                else if (obj is bool)
                {
                    totalLen += 1;
                }
                else throw new Exception("Unsupported type!");
            }

            /// Store format: Key + Type + Value.  Type is one byte length. 
            totalLen += _writeDatas.Count;
            if (totalLen > ushort.MaxValue) throw new Exception("Data exceed max size!");

            /// Write to buffer
            ByteBuffer buffer = bufferCreator(totalLen);
            buffer.position = buffer.position + reservedSize;

            foreach (var entry in _writeDatas)
            {
                byte[] key = entry.Key;
                Object obj = entry.Value;

                Debugger.Assert(key.Length <= 256);
                buffer.WriteByte((byte)key.Length);
                buffer.WriteBytes(key);

                if (obj is int)
                {
                    buffer.WriteByte(_DataTypes.INT);
                    buffer.WriteInt((int)obj);
                }
                else if (obj is byte[])
                {
                    byte[] bytes = (byte[])obj;
                    Debugger.Assert(bytes.Length <= 256);

                    buffer.WriteByte(_DataTypes.STRING);
                    buffer.WriteUShort((ushort)bytes.Length);
                    buffer.WriteBytes(bytes);
                }
                else if (obj is bool)
                {
                    buffer.WriteByte(_DataTypes.BOOL);
                    bool b = (bool)obj;
                    buffer.WriteByte((byte)(b ? 1 : 0));
                }
                else throw new Exception("Unsupported type!");
            }

            return buffer;
        }

        public void Deserialize(ByteBuffer buffer)
        {
            _readDatas = new Dictionary<String, Object>();

            while (buffer.HashRemaining(true))
            {
                /// Read key
                byte keyLen = buffer.FReadByte();
                String key = buffer.FReadString(keyLen);
                /// Read value type
                byte type = buffer.FReadByte();

                /// Read value
                if (type == _DataTypes.INT)
                {
                    _readDatas.Add(key, buffer.FReadInt());
                }
                else if (type == _DataTypes.STRING)
                {
                    int strLen = buffer.FReadUShort();
                    _readDatas.Add(key, buffer.FReadString(strLen));
                }
                else if (type == _DataTypes.BOOL)
                {
                    _readDatas.Add(key, buffer.FReadByte() == 1 ? true : false);
                }
                else throw new Exception("Corruppted data!");
            }
        }

        override public String ToString()
        {
            if (_readDatas != null)
            {
                return JsonWriter.Serialize(_readDatas);
            }
            if (_writeDatas != null)
            {
                return JsonWriter.Serialize(_writeDatas);
            }
            return "Empty";
        }

        public byte GetFormat()
        {
            return _DataTypes.SIMPLE_OBJECT;
        }

    }

}
