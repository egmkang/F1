using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Google.Protobuf;
using MessagePack;
using Ceras;
using F1.Abstractions.RPC;

namespace F1.Core.RPC
{
    public class ParametersSerializerCeras  : IParametersSerializer
    {
        private readonly CerasSerializer serializer = new CerasSerializer();

        public ParametersSerializerCeras() 
        {
            this.serializer.GetConfig().VersionTolerance.Mode = VersionToleranceMode.Extended;
        }

        public byte[] Serialize(object[] p, Type[] types) 
        {
            return this.serializer.Serialize(p);
        }

        public object[] Deserialize(byte[] bytes, Type[] types)
        {
            return this.serializer.Deserialize<object[]>(bytes);
        }

        public byte[] Serialize(object p, Type type)
        {
            return this.serializer.Serialize(p);
        }

        public object Deserialize(byte[] bytes, Type type)
        {
            return this.serializer.Deserialize<object>(bytes);
        }
    }

    public class ParametersSerializerMsgPack : IParametersSerializer
    {
        static byte[] Empty = new byte[0];
        public byte[] Serialize(object[] p, Type[] types) 
        {
            using var memoryStream = new MemoryStream(128);
            using var stream = new CodedOutputStream(memoryStream);

            stream.WriteInt32(p.Length);
            for (int i = 0; i < p.Length; ++i)
            {
                var data = p[i] == null ? Empty : MessagePackSerializer.Serialize(types[i], p[i]);
                stream.WriteBytes(ByteString.CopyFrom(data));
            }
            stream.Flush();
            memoryStream.Flush();

            return memoryStream.ToArray();
        }

        public object[] Deserialize(byte[] p, Type[] types)
        {
            using var memoryStream = new MemoryStream(p);
            using var stream = new CodedInputStream(memoryStream);

            var length = stream.ReadInt32();
            var o = new object[length];
            for (int i = 0; i < length; ++i)
            {
                var bytes = stream.ReadBytes().ToByteArray();
                o[i] = bytes.Length == 0 ? null : MessagePackSerializer.Deserialize(types[i], bytes);
            }

            return o;
        }

        public byte[] Serialize(object p, Type type)
        {
            if (p == null) return Empty;
            return MessagePackSerializer.Serialize(type, p);
        }

        public object Deserialize(byte[] bytes, Type type)
        {
            return MessagePackSerializer.Deserialize(type, bytes);
        }
    }
}
