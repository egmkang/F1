using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Network;
using F1.Core.Message;
using F1.Core.Network;

namespace F1.Gateway
{
    internal class GatewayMessageHandler
    {
        public readonly ILogger logger;
        public readonly IMessageCenter messageCenter;

        public GatewayMessageHandler(ILoggerFactory loggerFactory, IMessageCenter messageCenter) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Gateway");
            this.messageCenter = messageCenter;
        }

        public void RegisterMessageCallback(IMessageCenter messageCenter)
        {
            messageCenter.RegisterMessageProc(BlockMessageCodec.MessageName, GatewayIncommingMessage);
        }


        private void GatewayIncommingMessage(InboundMessage inboundMessage) 
        {
            var sessionInfo = inboundMessage.SourceConnection.GetSessionInfo();

        }
        private async Task ProcessGatewayMessageSlow() 
        {
        }
    }
}
