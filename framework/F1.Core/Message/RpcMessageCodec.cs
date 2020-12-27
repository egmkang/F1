using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using DotNetty.Buffers;
using Google.Protobuf;
using F1.Abstractions.Network;
using F1.Core.Utils;

namespace F1.Core.Message
{
    internal partial class Constants
    {
        public const int RpcMagic = 0x46464646;    //4个字符F
        public const int RpcHeaderLength = 4 + 4 + 4;
        public const int RpcMetaMinLength = 1;
        public const int RpcMetaMaxLength = 1 << 30;
        public const int RpcBodyMaxLength = 1 << 30;
    }

    public class RpcMessage
    {
        private static readonly byte[] EmptyBody = new byte[0];
        public RpcMessage() 
        {
            this.Body = EmptyBody;
        }
        public IMessage Meta { get; set; }
        public byte[] Body { get; set; }
    }

    internal class RpcMessageEncoder 
    {
        //TODO
        //LZ4
        public IByteBuffer Encode(IByteBufferAllocator bufferAllocator, RpcMessage rpcMessage)
        {
            var metaMessage = rpcMessage.Meta;
            var metaBodySize = metaMessage.CalculateSize();
            var metaMessageName = StringMap.GetStringBytes(metaMessage.Descriptor.FullName);
            var metaLength = 1 + metaMessageName.Length + metaBodySize;

            var bodyLength = rpcMessage.Body.Length;

            var totalLength = Constants.RpcHeaderLength + metaLength + bodyLength;

            var buffer = bufferAllocator.Buffer(totalLength);
            buffer.WriteIntLE(Constants.RpcMagic);
            buffer.WriteIntLE(metaLength);
            buffer.WriteIntLE(bodyLength);
            metaMessage.EncodeProtobufMessage(buffer, metaBodySize);
            buffer.WriteBytes(rpcMessage.Body);

            return buffer;
        }
    }

    internal class RpcMessageDecoder 
    {
        static readonly byte[] EmptyArray = new byte[0];

        public (long length, RpcMessage msg) Decode(IByteBuffer input)
        {
            if (input.ReadableBytes > Constants.RpcHeaderLength)
            {
                input.MarkReaderIndex();
                var magic = input.ReadIntLE();
                if (magic != Constants.RpcMagic) 
                {
                    throw new Exception($"Rpc Bad Magic:{magic}");
                }
                var metaLength = input.ReadIntLE();
                var bodyLength = input.ReadIntLE();
                if (metaLength <= Constants.RpcMetaMinLength)
                {
                    throw new Exception($"Rpc Meta Length:{metaLength} <= {Constants.RpcMetaMinLength}");
                }
                if (metaLength >= Constants.RpcMetaMaxLength)
                {
                    throw new Exception($"Rpc Meta Length:{metaLength} Out of Bound");
                }
                if (bodyLength < 0) 
                {
                    throw new Exception($"Rpc Body Length:{bodyLength}");
                }
                if (bodyLength >= Constants.RpcBodyMaxLength)
                {
                    throw new Exception($"Rpc Body Length:{bodyLength} Out of Bound");
                }
                if (input.ReadableBytes < metaLength + bodyLength)
                {
                    input.ResetReaderIndex();
                    return (0, null);
                }

                var (_, message) = input.DecodeProtobufMessage(metaLength);

                var array = bodyLength != 0 ? new byte[bodyLength] : EmptyArray;
                if (bodyLength != 0) 
                {
                    input.ReadBytes(array);
                }

                return (Constants.RpcHeaderLength + metaLength + bodyLength, new RpcMessage()
                {
                    Meta = message,
                    Body = array,
                });
            }

            input.ResetReaderIndex();
            return (0, null);
        }
    }

    /// <summary>
    /// 4字节"FFFF"
    /// 4字节小端MetaLength
    /// 4字节小端BodyLength
    /// MetaLength字节RpcMeta序列化好的内容
    /// BodyLength字节byte[]
    /// </summary>
    internal sealed class RpcMessageCodec : IMessageCodec
    {
        private readonly RpcMessageEncoder encoder = new RpcMessageEncoder();
        private readonly RpcMessageDecoder decoder = new RpcMessageDecoder();

        public string CodecName => "RpcMessageCodec";

        public (long length, string typeName, object msg) Decode(IByteBuffer input)
        {
            var (length, msg) = decoder.Decode(input);
            return (length, msg?.Meta?.Descriptor.FullName, msg);
        }

        public IByteBuffer Encode(IByteBufferAllocator allocator, object msg)
        {
            Contract.Assert((msg as RpcMessage) != null, msg.GetType().FullName);
            return encoder.Encode(allocator, msg as RpcMessage);
        }
    }
}
