using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Google.Protobuf;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Core.Message;

namespace F1.Core.Network
{
    public sealed class DefaultChannelSessionInfo : IConnectionSessionInfo
    {
        private static readonly UnboundedChannelOptions options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        };

        private readonly long sessionID;
        private long activeTime;
        private int stop = 0;
        private int queueCount = 0;
        private IPEndPoint address;
        private readonly Channel<IOutboundMessage> queue;
        private readonly ChannelWriter<IOutboundMessage> writer;
        private readonly ILogger logger;
        private readonly IMessageCenter messageCenter;

        public DefaultChannelSessionInfo(long sessionID, ILogger logger, IMessageCenter messageCenter)
        {
            this.sessionID = sessionID;
            this.logger = logger;
            this.messageCenter = messageCenter;

            this.queue = Channel.CreateUnbounded<IOutboundMessage>(options);
            this.writer = queue.Writer;
        }

        public long SessionID => sessionID;

        public long ActiveTime { get => activeTime; set => activeTime = value; }
        public IPEndPoint RemoteAddress { get => address; set => address = value; }
        public int PutOutboundMessage(IOutboundMessage msg)
        {
            var v = Interlocked.Increment(ref this.queueCount);
            if (!this.writer.TryWrite(msg))
            {
                Interlocked.Decrement(ref this.queueCount);
                v--;
                logger.LogInformation("PutOutboundMessage, SessionID:{0}, queue has been closed", this.sessionID);
            }
            return v;
        }
        public int SendingQueueCount => this.queueCount;
        public void ShutDown() 
        {
            this.stop = 1;
            this.writer.TryWrite(null);
            this.writer.Complete();
        }

        public void RunSendLoopAsync(IChannel channel)
        {
            var allocator = channel.Allocator;
            var reader = this.queue.Reader;
            Task.Run(async () => 
            {
                while (this.stop == 0) 
                {
                    var more = await reader.WaitToReadAsync();
                    if (!more) 
                    {
                        break;
                    }

                    IOutboundMessage message = default;
                    var number = 0;
                    try 
                    {
                        while (number < 4 && reader.TryRead(out message)) 
                        {
                            Interlocked.Decrement(ref this.queueCount);
                            var msg = message.Inner as IMessage;
                            var buffer = msg.ToByteBuffer(allocator);
                            await channel.WriteAsync(buffer);
                            number++;
                        }
                        channel.Flush();
                        number = 0;
                    }
                    catch (Exception e)  when(message != default)
                    {
                        logger.LogError("SendOutboundMessage Fail, SessionID:{0}, Exception:{1}",
                            this.sessionID, e.Message);
                        this.messageCenter.OnMessageFail(message);
                    }
                }
                this.logger.LogInformation("SessionID:{0}, SendingLoop Exit", this.sessionID);
            });
        }
    }
}
