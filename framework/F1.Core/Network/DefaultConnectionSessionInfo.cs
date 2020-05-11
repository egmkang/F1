using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Core.Message;
using F1.Core.Utils;

namespace F1.Core.Network
{
    public sealed class DefaultConnectionSessionInfo : IConnectionSessionInfo
    {
        private readonly long sessionID;
        private long activeTime;
        private bool stop = false;
        private IPEndPoint address;
        private readonly AsyncMessageQueue<OutboundMessage> inboundMessageQueue;
        private readonly ILogger logger;
        private readonly IMessageCenter messageCenter;
        private readonly IMessageCodec codec;

        public DefaultConnectionSessionInfo(long sessionID, ILogger logger, IMessageCenter messageCenter, IMessageCodec codec)
        {
            this.sessionID = sessionID;
            this.logger = logger;
            this.messageCenter = messageCenter;
            this.codec = codec;

            this.inboundMessageQueue = new AsyncMessageQueue<OutboundMessage>();

            this.ActiveTime = Platform.GetMilliSeconds();
        }

        public long SessionID => sessionID;

        public long ActiveTime { get => activeTime; set => activeTime = value; }
        public IPEndPoint RemoteAddress { get => address; set => address = value; }
        public long ServerID { get; set; }
        public bool IsActive => !this.stop;

        public int PutOutboundMessage(OutboundMessage msg)
        {
            if (!this.inboundMessageQueue.PushMessage(msg)) 
            {
                logger.LogWarning("SessionID:{0}, Drop Message", this.sessionID);
            }
            return this.inboundMessageQueue.QueueCount;
        }

        public void ShutDown() 
        {
            this.stop = true;
            this.inboundMessageQueue.ShutDown(OutboundMessage.Empty);
        }

        public void RunSendingLoopAsync(IChannel channel)
        {
            var allocator = channel.Allocator;
            var reader = this.inboundMessageQueue.Reader;
            Task.Run(async () => 
            {
                while (this.inboundMessageQueue.Valid) 
                {
                    var more = await reader.WaitToReadAsync();
                    if (!more) 
                    {
                        break;
                    }

                    OutboundMessage message;
                    var number = 0;
                    try 
                    {
                        while (number < 4 && reader.TryRead(out message) && message.Inner != null)
                        {
                            this.inboundMessageQueue.QueueCount--;
                            var buffer = this.codec.Encode(allocator, message.Inner);
                            _ = channel.WriteAsync(buffer);
                            number++;
                        }
                        channel.Flush();
                        number = 0;
                    }
                    catch (Exception e)  when(message.Inner != default)
                    {
                        logger.LogError("SendOutboundMessage Fail, SessionID:{0}, Exception:{1}",
                            this.sessionID, e.Message);
                        this.messageCenter.OnMessageFail(message);
                    }

                    await Task.Yield();
                }
                this.logger.LogInformation("SessionID:{0}, SendingLoop Exit", this.sessionID);
            });
        }
    }
}
