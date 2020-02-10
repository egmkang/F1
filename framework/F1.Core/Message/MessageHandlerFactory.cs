using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using DotNetty.Codecs;
using F1.Abstractions.Network;

namespace F1.Core.Message
{
    public sealed class MessageHandlerFactory : IMessageHandlerFactory
    {
        private ILoggerFactory loggerFactory;
        private IServiceProvider serviceProvider;
        private IMessageCenter messageCenter;

        public MessageHandlerFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IMessageCenter messageCenter)
        {
            this.messageCenter = messageCenter;
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
        }

        public ByteToMessageDecoder NewHandler()
        {
            return new MessageHandler(this.serviceProvider, this.loggerFactory, this.messageCenter);
        }
    }
}
