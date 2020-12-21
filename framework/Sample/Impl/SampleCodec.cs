using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using F1.Core.Message;
using F1.Core.Utils;
using Google.Protobuf;

namespace F1.Sample.Impl
{
    public class SampleCodec
    {

        public IMessage DecodeMessage(byte[] data) 
        {
            var buffer = Unpooled.WrappedBuffer(data);
            var nameLength = buffer.ReadByte();
            var messageName = buffer.ReadString(nameLength, Encoding.UTF8);
            var bodyBuffer = buffer.ReadSlice((int)(data.Length - 1 - nameLength));

            try
            {
                var message = MessageExt.NewMessage(messageName, bodyBuffer);
                return message;
            }
            catch
            {
                throw;
            }
            finally
            {
                buffer.Release();
            }
        }

        public byte[] EncodeMessage(IMessage msg) 
        {
            var bodySize = msg.CalculateSize();
            var messageName = Encoding.UTF8.GetBytes(msg.Descriptor.FullName);
            var length = 1 + messageName.Length + bodySize;

            var bytes = new byte[length];
            var buffer = Unpooled.WrappedBuffer(bytes);
            buffer.SetWriterIndex(0);
            buffer.SetReaderIndex(0);

            buffer.WriteByte(messageName.Length);
            buffer.WriteBytes(messageName);

            ArraySegment<byte> data = buffer.GetIoBuffer(buffer.WriterIndex, bodySize);
            Span<byte> span = data;
            msg.WriteTo(span);

            buffer.SetWriterIndex(buffer.WriterIndex + bodySize);

            buffer.Release();

            return bytes; ;
        }
    }
}
