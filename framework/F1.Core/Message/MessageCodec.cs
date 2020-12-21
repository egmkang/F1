using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using F1.Abstractions.Network;
using F1.Core.Utils;
using Google.Protobuf;


namespace F1.Core.Message
{
    internal partial class Constants 
    {
        public const int HeaderLength = 8;
        public const long MaxFrameLength = 1L << 32;
        public const long MinFrameLength = HeaderLength;
    }

    internal class MessageEncoder
    {
        public IByteBuffer Encode(IByteBufferAllocator bufferAllocator, IMessage message) 
        {
            var bodySize = message.CalculateSize();
            var messageName = Encoding.UTF8.GetBytes(message.Descriptor.FullName);
            var length = Constants.HeaderLength + 1 + messageName.Length + bodySize;

            var buffer = bufferAllocator.Buffer(length);
            buffer.WriteLongLE(length);
            buffer.WriteByte(messageName.Length);
            buffer.WriteBytes(messageName);
            
            ArraySegment<byte> data = buffer.GetIoBuffer(buffer.WriterIndex, bodySize);
            Span<byte> span = data;
            message.WriteTo(span);

            buffer.SetWriterIndex(buffer.WriterIndex + bodySize);

            return buffer;
        }
    }

    internal class MessageDecoder 
    {
        public (long length, IMessage msg) Decode(IByteBuffer input)
        {
            if (input.ReadableBytes >= Constants.HeaderLength)
            {
                input.MarkReaderIndex();
                var length = input.ReadLongLE();
                if (length < Constants.HeaderLength) 
                {
                    throw new Exception($"Message Header Length:{length} < 8");
                }
                if (length >= Constants.MaxFrameLength) 
                {
                    throw new Exception($"Message Length:{length} Out of Bound");
                }
                if (input.ReadableBytes < length - Constants.HeaderLength) 
                {
                    input.ResetReaderIndex();
                    return (0, null);
                }
                var messageNameLength = input.ReadByte();
                var messageName = input.ReadString(messageNameLength, Encoding.UTF8);
                var bodyBuffer = input.ReadBytes((int)(length - Constants.HeaderLength - 1 - messageNameLength));

                try
                {
                    var message = MessageExt.NewMessage(messageName, bodyBuffer);
                    return (length, message);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    bodyBuffer.Release();
                }
            }

            input.ResetReaderIndex();
            return (0, null);
        }
    }

    internal sealed class ProtobufMessageCodec : IMessageCodec
    {
        private readonly MessageEncoder encoder = new MessageEncoder();
        private readonly MessageDecoder decoder = new MessageDecoder();

        public string CodecName => "ProtobufMessageCodec";

        public (long length, string typeName, object msg) Decode(IByteBuffer input)
        {
            var (length, msg) = decoder.Decode(input);
            return (length, msg?.Descriptor.FullName, msg);
        }

        public IByteBuffer Encode(IByteBufferAllocator allocator, object msg)
        {
            return encoder.Encode(allocator, msg as IMessage);
        }
    }


    public sealed class StringMessageCodec : IMessageCodec
    {
        public string CodecName => "StringMessageCodec";

        public (long length, string typeName, object msg) Decode(IByteBuffer input)
        {
            var length = input.ReadableBytes;
            var typeName = "String";
            var msg = input.ReadString(length, Encoding.UTF8);
            return (length, typeName, msg);
        }

        public IByteBuffer Encode(IByteBufferAllocator allocator, object msg)
        {
            var buffer = allocator.Buffer();
            buffer.WriteString(msg.ToString(), Encoding.UTF8);
            return buffer;
        }
    }

    /// <summary>
    /// 4字节包头(小端)
    /// 低3字节表示包体长度
    /// 高1字节保留, 暂时没做加密和压缩支持
    /// </summary>
    public sealed class BlockMessageCodec : IMessageCodec
    {
        public const uint Mask = (1 << 24) - 1;
        public const int HeaderLength = sizeof(uint);
        public const string MessageName = "BlockMessage";
        public int MaxPacketLength { get; set; } = 1 * 1024 * 1024;

        public string CodecName => "BlockMessageCodec";

        public (long length, string typeName, object msg) Decode(IByteBuffer input)
        {
            if (input.ReadableBytes < HeaderLength) return (0, "", null);
            input.MarkReaderIndex();

            var header = input.ReadUnsignedIntLE();
            var length = header & Mask;
            if (input.ReadableBytes < length) 
            {
                input.ResetReaderIndex();
                return (0, "", null);
            }
            if (length >= MaxPacketLength) 
            {
                throw new Exception($"Message Length:{length} Out of Bound");
            }

            var bytes = new byte[length];
            input.ReadBytes(bytes);
            return (length + HeaderLength, MessageName, bytes);
        }

        public IByteBuffer Encode(IByteBufferAllocator allocator, object msg)
        {
            var bytes = msg as byte[];
            var length = bytes != null ? bytes.Length : 0;
            var buffer = allocator.Buffer(HeaderLength + length);
            buffer.WriteIntLE(length);
            if (length > 0) 
            {
                buffer.WriteBytes(bytes);
            }
            return buffer;
        }
    }
}
