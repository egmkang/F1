using System;
using Xunit;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using F1.Core.Message;
using RpcMessage;

namespace F1.Core.Test.Network
{
    public class Codec
    {
        static IByteBufferAllocator Allocator = PooledByteBufferAllocator.Default;
        static Random random = new Random();

        [Fact]
        public void EmptyMessage()
        {
            var msg = new RequestRpcHandshake();
            var buffer = msg.ToByteBuffer(Allocator);

            var (length, msg1) = buffer.DecodeOneMessage();

            Assert.Equal(buffer.ReadableBytes, 0);
            Assert.NotEqual(length, 0);

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void HalfPacket() 
        {
            var msg = new RequestRpcHandshake();
            var buffer = msg.ToByteBuffer(Allocator);

            var half = buffer.Slice(0, buffer.ReadableBytes - 1);

            var (length, msg1) = half.DecodeOneMessage();

            Assert.NotEqual(half.ReadableBytes, 0);
            Assert.Equal(length, 0);
            Assert.Null(msg1);

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void RandomMessage() 
        {
            var msg = new RequestRpcHandshake();
            msg.ServerId = random.Next(0, 1000000);
            var buffer = msg.ToByteBuffer(Allocator);

            var (length, msg1) = buffer.DecodeOneMessage();

            Assert.Equal(buffer.ReadableBytes, 0);
            Assert.NotEqual(length, 0);
            Assert.NotNull(msg1);
            Assert.IsType(msg.GetType(), msg1);
            Assert.Equal(msg, (msg1 as RequestRpcHandshake));

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void TowMessages()
        {
            var input1 = new RequestRpcHandshake();
            input1.ServerId = random.Next(0, 1000000);

            var input2 = new ResponseRpcHandshake();
            input2.StartTime = random.Next(0, 10000000);
            input2.ServerId = random.Next(0, 100000);

            var buffer1 = input1.ToByteBuffer(Allocator);
            var buffer2 = input2.ToByteBuffer(Allocator);

            var buffer = Allocator.Buffer(buffer1.ReadableBytes + buffer2.ReadableBytes);
            buffer.WriteBytes(buffer1);
            buffer.WriteBytes(buffer2);

            var (length1, msg1) = buffer.DecodeOneMessage();
            var (length2, msg2) = buffer.DecodeOneMessage();

            Assert.Equal(buffer.ReadableBytes, 0);
            Assert.NotEqual(length1, 0);
            Assert.NotEqual(length2, 0);
            Assert.NotNull(msg1);
            Assert.IsType(input1.GetType(), msg1);
            Assert.Equal(input1, (msg1 as RequestRpcHandshake));

            Assert.NotNull(msg2);
            Assert.IsType(input2.GetType(), msg2);
            Assert.Equal(input2, (msg2 as ResponseRpcHandshake));

            ReferenceCountUtil.Release(buffer1);
            ReferenceCountUtil.Release(buffer2);
            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void MinimumPacket() 
        {
            var buffer = Allocator.Buffer();
            buffer.WriteLongLE(7);
            buffer.WriteLong(7);

            Assert.Throws<Exception>(() =>
            {
                var (length, msg) = buffer.DecodeOneMessage();
            });

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void MaximumPackhet() 
        {
            var buffer = Allocator.Buffer();
            buffer.WriteLongLE(1L << 32 + 1);
            buffer.WriteLong(7);

            Assert.Throws<Exception>(() =>
            {
                var (length, msg) = buffer.DecodeOneMessage();
            });

            ReferenceCountUtil.Release(buffer);
        }
    }
}
