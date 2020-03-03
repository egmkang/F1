using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Google.Protobuf;
using MessagePack;

namespace F1.Core.RPC
{
    public class ParametersSerializerBinary
    {
        private readonly IFormatter formatter = new BinaryFormatter();
        public byte[] Serializer(object[] p) 
        {
            using var stream = new MemoryStream(128);
            this.formatter.Serialize(stream, p);
            stream.Flush();
            return stream.ToArray();
        }

        public object[] Deserialize(byte[] p)
        {
            using var stream = new MemoryStream(p);
            return (object[])this.formatter.Deserialize(stream);
        }
    }

    public class ParametersSerializerMsgPack 
    {
        static byte[] Empty = new byte[0];
        public byte[] Serializer(object[] p) 
        {
            using var memoryStream = new MemoryStream(128);
            using var stream = new CodedOutputStream(memoryStream);

            stream.WriteInt32(p.Length);
            for (int i = 0; i < p.Length; ++i)
            {
                var data = p[i] == null ? Empty : MessagePackSerializer.Serialize(p[i].GetType(), p[i]);
                stream.WriteBytes(ByteString.CopyFrom(data));
            }
            stream.Flush();
            memoryStream.Flush();

            return memoryStream.ToArray();
        }

        public object[] Deserialize(byte[] p, Type[] t)
        {
            using var memoryStream = new MemoryStream(p);
            using var stream = new CodedInputStream(memoryStream);

            var length = stream.ReadInt32();
            var o = new object[length];
            for (int i = 0; i < length; ++i)
            {
                var bytes = stream.ReadBytes().ToByteArray();
                o[i] = bytes.Length == 0 ? null : MessagePackSerializer.Deserialize(t[i], bytes);
            }

            return o;
        }
    }
}
