using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

using BaseLib;

using JsonFx.Json;

namespace Actor.Serializable
{
    /// Wrap the protobuf protocol.
    public class PojoObject : ISerializableData
    {
        public static void RegisterAssembly(Assembly assembly) { _Assembly = assembly; }
        protected static Assembly _Assembly = Assembly.GetEntryAssembly();

        private Object _object;

        public PojoObject() { }

        public PojoObject(Object obj)
        {
            _object = obj;
        }

        public T GetObject<T>()
        {
            return (T)_object;
        }

        public ByteBuffer Serialize(int reservedSize, Func<int, ByteBuffer> bufferCreator)
        {
            byte[] clsNameBytes = Encoding.UTF8.GetBytes(_object.GetType().FullName);
            Debugger.Assert(clsNameBytes.Length <= 256);

            byte[] bytes = ProtoBufUtil.Serialize(_object);

            /// Calculate total length count
            int totalLen = reservedSize + (1 + clsNameBytes.Length) + (2 + bytes.Length);
            Debugger.Assert(totalLen <= ushort.MaxValue);

            /// Write to buffer
            ByteBuffer buffer = bufferCreator(totalLen);
            buffer.position = buffer.position + reservedSize;

            buffer.WriteByte((byte)clsNameBytes.Length);
            buffer.WriteBytes(clsNameBytes);

            buffer.WriteUShort((ushort)bytes.Length);
            buffer.WriteBytes(bytes);

            return buffer;
        }

        public void Deserialize(ByteBuffer buffer)
        {
            int nameLen = buffer.FReadByte();
            string clsName = buffer.FReadString(nameLen);

            Type clazz = _Assembly.GetType(clsName);
            if (null == clazz) throw new Exception("PojoObject: Type " + clsName + " not found in assembly!");

            int byteLen = buffer.FReadUShort();
            byte[] bytes = buffer.FReadBytes(byteLen);

            _object = ProtoBufUtil.Deserialize(bytes, clazz);
        }

        override public String ToString()
        {
            if (null == _object) return "Empty";
            return JsonWriter.Serialize(_object);
        }

        public byte GetFormat()
        {
            return _DataTypes.POJO_OBJECT;
        }
    }

}