using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;

namespace F1.Abstractions.Network
{
    public interface IMessageCodec 
    {
        IByteBuffer Encode(IByteBufferAllocator allocator, object msg);
        (long length, string typeName, object msg) Decode(IByteBuffer input);
    }

    public interface IMessageHandlerFactory
    {
        ByteToMessageDecoder NewHandler();

        IMessageCodec Codec { get; set; }
    }
}
