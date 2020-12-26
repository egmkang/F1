using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using Google.Protobuf;
using F1.Core.Utils;


namespace F1.Core.Message
{
    internal static class CodecExtensions
    {
        //1字节MessageNameLength
        //N字节UTF8 MessageName
        //bodySize字节ProtobufMessage
        public static IByteBuffer EncodeProtobufMessage(this IMessage message, IByteBuffer output, int bodySize) 
        {
            var messageName = StringMap.GetStringBytes(message.Descriptor.FullName);

            output.WriteByte(messageName.Length);
            output.WriteBytes(messageName);

            Span<byte> span = output.GetIoBuffer(output.WriterIndex, bodySize);
            message.WriteTo(span);

            output.SetWriterIndex(output.WriterIndex + bodySize);

            return output;
        }

        public static (int, IMessage) DecodeProtobufMessage(this IByteBuffer input, int length) 
        {
            var messageNameLength = input.ReadByte();
            var messageName = input.ReadString(messageNameLength, Encoding.UTF8);
            var readLength = (int)(length - 1 - messageNameLength);
            var bodyBuffer = input.IoBufferCount == 1 ? input.ReadSlice(readLength) : input.ReadBytes(readLength);

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
                if (input.IoBufferCount != 1) 
                {
                    bodyBuffer.Release();
                }
            }
        }
    }
}
