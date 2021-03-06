using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using DotNetty.Codecs;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Handlers.Timeout;
using F1.Core.Utils;
using F1.Core.Network;
using F1.Abstractions.Network;

namespace F1.Core.Message
{
    internal sealed class MessageHandler : ByteToMessageDecoder
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IMessageCenter messageCenter;
        private readonly IMessageCodec codec;


        public MessageHandler(IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IMessageCodec codec)
        {
            this.messageCenter = messageCenter;
            this.serviceProvider = serviceProvider;
            this.logger = loggerFactory.CreateLogger("F1.Sockets");
            this.codec = codec;
        }

        public override bool IsSharable => false;
        
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            var currentMilliSeconds = Platform.GetMilliSeconds();
            var sessionInfo = context.Channel.GetSessionInfo();

            while (input.ReadableBytes > 0)
            {
                var (length, typeName, message) = this.codec.Decode(input);
                if (length == 0)
                {
                    break;
                }
                if (message == null) 
                {
                    logger.LogError("Decode Fail, SessionID:{0}", context.Channel.GetSessionInfo().SessionID);
                    break;
                }

                //this.logger.LogTrace("DecodeMessage:{0}", typeName);

                sessionInfo.ActiveTime = currentMilliSeconds;

                var inboundMessage = new InboundMessage(context.Channel, typeName, message, Platform.GetMilliSeconds());
                this.messageCenter.OnReceiveMessage(inboundMessage);
            }
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            var sessionInfo = ctx.Channel.GetSessionInfo();

            this.logger.LogInformation("SessionID:{0} Inactive", sessionInfo.SessionID);

            this.messageCenter.OnConnectionClosed(ctx.Channel);
            sessionInfo.ShutDown();

            base.ChannelInactive(ctx);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            var sessionInfo = context.Channel.GetSessionInfo();
            logger.LogError("SessionID:{0}, Exception:{1}", sessionInfo.SessionID, exception.Message);
            context.CloseAsync();
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            var sessionInfo = context.Channel.GetSessionInfo();

            if (evt is IdleStateEvent && (evt as IdleStateEvent).State == IdleState.ReaderIdle)
            {
                logger.LogError("SessionID:{0} TimeOut, Close", sessionInfo.SessionID);
                context.CloseAsync();
            }
            else
            {
                base.UserEventTriggered(context, evt);
            }
        }
    }
}
