using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using DotNetty.Buffers;
using F1.Core.Network;
using F1.Core.Utils;
using F1.Abstractions.Network;

namespace F1.Core.Message
{
    internal class MessageHandler : ByteToMessageDecoder
    {
        private static readonly MessageDecoder decoder = new MessageDecoder();
        private ILogger logger;
        private IServiceProvider serviceProvider;
        private IMessageCenter messageCenter;

        public MessageHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IMessageCenter messageCenter)
        {
            this.messageCenter = messageCenter;
            this.serviceProvider = serviceProvider;
            this.logger = loggerFactory.CreateLogger("F1.Sockets");
        }

        public override bool IsSharable => false;
        
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            var currentMilliSeconds = Platform.GetMilliSeconds();
            var sessionInfo = context.Channel.GetSessionInfo();

            while (input.ReadableBytes >= Constants.HeaderLength)
            {
                var (length, message) = decoder.Decode(input);
                if (length == 0)
                {
                    break;
                }
                if (message == null) 
                {
                    logger.LogError("Decode Fail, SessionID:{0}", context.Channel.GetSessionInfo().SessionID);
                    break;
                }
                sessionInfo.ActiveTime = currentMilliSeconds;

                var inboundMessage = new InboundMessage(context.Channel, message);
                this.messageCenter.OnReceivedMessage(inboundMessage);
            }
        }
    }
}
