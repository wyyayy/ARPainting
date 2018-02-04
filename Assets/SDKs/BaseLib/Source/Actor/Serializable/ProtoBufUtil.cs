using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using ProtoBuf;

namespace Actor.Serializable
{

    public class ProtoBufUtil
    {
        /*
        private static Map<Class<?>, Schema<?>> cachedSchema = new ConcurrentHashMap<Class<?>, Schema<?>>();

        private static <T> Schema<T> GetSchema(Class<T> clazz)
        {
            @SuppressWarnings("unchecked")
            Schema<T> schema = (Schema<T>) cachedSchema.get(clazz);
            if (schema == null)
            {
                schema = RuntimeSchema.getSchema(clazz);
                if (schema != null)
                {
                    cachedSchema.put(clazz, schema);
                }
            }
            return schema;
        }
        */

        public static byte[] Serialize(Object obj)
        {
            using (MemoryStream memoryStrean = new MemoryStream())
            {
                Serializer.Serialize(memoryStrean, obj);
                byte[] bytes = new byte[memoryStrean.Length];
                memoryStrean.Position = 0;
                memoryStrean.Read(bytes, 0, bytes.Length);

                return bytes;
            }
        }

        public static object Deserialize(byte[] bytes, Type clazz)
        {
            using (MemoryStream memoryStrean = new MemoryStream(bytes, false))
            {
                return Serializer.Deserialize(clazz, memoryStrean);
            }
        }

    }

}