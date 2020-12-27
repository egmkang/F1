using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DotNetty.Buffers;
using Google.Protobuf;
using Google.Protobuf.Reflection;


namespace F1.Core.Message
{
    public static partial class MessageExt
    {
        static Dictionary<string, MessageParser> parsers = new Dictionary<string, MessageParser>();

        static public IMessage NewMessage(string messageName, IByteBuffer buffer)
        {
            parsers.TryGetValue(messageName, out var parser);
            if (parser == null) return null;

            int length = buffer.ReadableBytes;
            Stream inputStream = null;

            try
            {
                if (buffer.IoBufferCount == 1)
                {
                    ArraySegment<byte> bytes = buffer.GetIoBuffer(buffer.ReaderIndex, length);
                    return parser.ParseFrom(new ReadOnlySequence<byte>(bytes.Array, bytes.Offset, length));
                }
                else
                {
                    inputStream = new ReadOnlyByteBufferStream(buffer, false);
                    using (var codedInputStream = new CodedInputStream(inputStream)) 
                    {
                        return parser.ParseFrom(codedInputStream);
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                inputStream?.Dispose();
            }
        }

        public static void Load()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }
                    LoadParserFromAssembly(assembly);
                }
                catch
                {
                    //logger.Error("LoadAssembly, Exception:{0}, StackTrace:{1}", e.Message, e.StackTrace);
                }
            }
        }

        static void LoadParserFromAssembly(Assembly asm)
        {
            var types = asm.ExportedTypes;
            foreach (var typeInfo in types)
            {
                var attrParser = typeInfo.GetProperty("Parser");
                var attrDescriptor = typeInfo.GetProperty("Descriptor");
                if (attrParser == null) continue;
                try
                {
                    var parser = attrParser.GetValue("") as MessageParser;
                    var fullName = (attrDescriptor.GetValue("") as MessageDescriptor).FullName;
                    if (fullName.StartsWith("google.protobuf")) continue;
                    parsers.Add(String.Intern(fullName), parser);
                }
                catch { }
            }
        }

        static MessageDecoder decoder = new MessageDecoder();
        public static (long length, IMessage message) DecodeOneProtobufMessage(this IByteBuffer buffer) 
        {
            return decoder.Decode(buffer);
        }

        static MessageEncoder encoder = new MessageEncoder();
        public static IByteBuffer ToByteBuffer(this IMessage msg, IByteBufferAllocator allocator) 
        {
            return encoder.Encode(allocator, msg);
        }

        static RpcMessageCodec rpcCodec = new RpcMessageCodec();
        public static IByteBuffer ToByteBuffer(this RpcMessage msg, IByteBufferAllocator allocator) 
        {
            return rpcCodec.Encode(allocator, msg);
        }
        public static (long length, RpcMessage message) DecodeOneRpcMessage(this IByteBuffer buffer) 
        {
            var (length, _, obj) = rpcCodec.Decode(buffer);
            return (length, obj as RpcMessage);
        }    
    }
}
