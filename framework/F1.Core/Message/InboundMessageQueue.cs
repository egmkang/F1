using F1.Abstractions.Network;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace F1.Core.Message
{
    internal class InboundMessageQueue
    {
        private readonly Channel<IInboundMessage> channel = Channel.CreateUnbounded<IInboundMessage>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        private ILogger logger;

        public InboundMessageQueue(ILoggerFactory loggerFactory) 
        {
            logger = loggerFactory.CreateLogger("F1.InboundMessage");
        }

        public void PushMessage(IInboundMessage message) 
        {
            if (!channel.Writer.TryWrite(message)) 
            {
                this.logger.LogError("Dropping InboundMessage, because inbound queue is closed");
            }
        }

        public void ShutDown() 
        {
            this.channel.Writer.TryWrite(null);
            this.channel.Writer.Complete();
            this.logger.LogInformation("InboundMessageQueue ShutDown");
        }

        public ChannelReader<IInboundMessage> Reader => this.channel.Reader;
    }
}
