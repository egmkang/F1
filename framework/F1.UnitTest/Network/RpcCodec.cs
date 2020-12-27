using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using Rpc;
using F1.Core.Message;


namespace F1.UnitTest.Network
{
    public class RpcCodec : Setup
    {
        static IByteBufferAllocator Allocator = PooledByteBufferAllocator.Default;
        static Random random = new Random();
        static RpcRequest requestMeta = new RpcRequest() 
        {
             ServiceName = "ITest",
             MethodName = "Func1",
        };
        static RpcResponse responseMeta = new RpcResponse() { };

        [Fact]
        public void EmptyBody() 
        {
            var body = new byte[0];
            var rpcMessage = new RpcMessage() 
            {
                 Meta =  requestMeta,
                 Body = body,
            };

            var buffer = rpcMessage.ToByteBuffer(Allocator);

            var (length, msg1) = buffer.DecodeOneRpcMessage();

            Assert.Equal(0, buffer.ReadableBytes);
            Assert.NotEqual(0, length);
            Assert.NotNull(msg1.Meta);
            Assert.NotNull(msg1.Body);
            Assert.Equal(rpcMessage.Meta, msg1.Meta);

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void NonEmptyBody() 
        {
            var body = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var rpcMessage = new RpcMessage() 
            {
                 Meta = requestMeta,
                 Body = body,
            };

            var buffer = rpcMessage.ToByteBuffer(Allocator);

            var (length, msg1) = buffer.DecodeOneRpcMessage();

            Assert.Equal(0, buffer.ReadableBytes);
            Assert.NotEqual(0, length);
            Assert.NotNull(msg1.Meta);
            Assert.NotNull(msg1.Body);
            Assert.Equal(rpcMessage.Meta, msg1.Meta);
            Assert.Equal(rpcMessage.Body, msg1.Body);

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void HeartBeat() 
        {
            var body = new byte[0];
            var rpcMessage = new RpcMessage()
            {
                Meta = new RpcHeartBeatRequest()
                {
                    RequestMilliseconds = 1,
                },
                Body = body,
            };

            var buffer = rpcMessage.ToByteBuffer(Allocator);

            var (length, msg1) = buffer.DecodeOneRpcMessage();

            Assert.Equal(0, buffer.ReadableBytes);
            Assert.NotEqual(0, length);
            Assert.NotNull(msg1.Meta);
            Assert.NotNull(msg1.Body);
            Assert.Equal(rpcMessage.Meta, msg1.Meta);

            ReferenceCountUtil.Release(buffer);
        }
    }
}
