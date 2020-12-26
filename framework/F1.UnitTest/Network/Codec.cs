using System;
using Xunit;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using F1.Core.Message;
using RpcProto;

namespace F1.UnitTest.Network
{
    public class Codec : Setup
    {
        static IByteBufferAllocator Allocator = PooledByteBufferAllocator.Default;
        static Random random = new Random();

        [Fact]
        public void EmptyMessage()
        {
            var msg = new RequestRpcHeartBeat();
            var buffer = msg.ToByteBuffer(Allocator);

            var (length, msg1) = buffer.DecodeOneProtobufMessage();

            Assert.Equal(0, buffer.ReadableBytes);
            Assert.NotEqual(0, length);

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void HalfPacket() 
        {
            var msg = new RequestRpcHeartBeat();
            var buffer = msg.ToByteBuffer(Allocator);

            var half = buffer.Slice(0, buffer.ReadableBytes - 1);

            var (length, msg1) = half.DecodeOneProtobufMessage();

            Assert.NotEqual(0, half.ReadableBytes);
            Assert.Equal(0, length);
            Assert.Null(msg1);

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void RandomMessage() 
        {
            var msg = new RequestRpcHeartBeat();
            msg.MilliSeconds = random.Next(0, 1000000);
            var buffer = msg.ToByteBuffer(Allocator);

            var (length, msg1) = buffer.DecodeOneProtobufMessage();

            Assert.Equal(0, buffer.ReadableBytes);
            Assert.NotEqual(0, length);
            Assert.NotNull(msg1);
            Assert.IsType(msg.GetType(), msg1);
            Assert.Equal(msg, (msg1 as RequestRpcHeartBeat));

            ReferenceCountUtil.Release(buffer);
        }

        [Fact]
        public void TowMessages()
        {
            var input1 = new RequestRpcHeartBeat();
            input1.MilliSeconds = random.Next(0, 1000000);

            var input2 = new RequestRpcHeartBeat();
            input2.MilliSeconds = random.Next(0, 10000000);

            var buffer1 = input1.ToByteBuffer(Allocator);
            var buffer2 = input2.ToByteBuffer(Allocator);

            var buffer = Allocator.Buffer(buffer1.ReadableBytes + buffer2.ReadableBytes);
            buffer.WriteBytes(buffer1);
            buffer.WriteBytes(buffer2);

            var (length1, msg1) = buffer.DecodeOneProtobufMessage();
            var (length2, msg2) = buffer.DecodeOneProtobufMessage();

            Assert.Equal(0, buffer.ReadableBytes);
            Assert.NotEqual(0, length1);
            Assert.NotEqual(0, length2);
            Assert.NotNull(msg1);
            Assert.IsType(input1.GetType(), msg1);
            Assert.Equal(input1, (msg1 as RequestRpcHeartBeat));

            Assert.NotNull(msg2);
            Assert.IsType(input2.GetType(), msg2);
            Assert.Equal(input2, (msg2 as RequestRpcHeartBeat));

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
                var (length, msg) = buffer.DecodeOneProtobufMessage();
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
                var (length, msg) = buffer.DecodeOneProtobufMessage();
            });

            ReferenceCountUtil.Release(buffer);
        }
    }
}
