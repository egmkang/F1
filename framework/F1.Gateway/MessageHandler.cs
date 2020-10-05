using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotNetty.Buffers;
using F1.Abstractions.Network;
using F1.Core.Message;
using F1.Core.Network;
using F1.Core.Utils;
using GatewayMessage;

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

            this.messageCenter.RegisterMessageProc(typeof(RequestHeartBeat).FullName, ProcessGatewayHeartBeat);
            this.messageCenter.RegisterMessageProc(typeof(RequestCloseConnection).FullName, ProcessGatewayCloseConnection);
            this.messageCenter.RegisterMessageProc(typeof(RequestSendMessageToPlayer).FullName, ProcessGatewaySendMessageToPlayer);
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

        private void ProcessGatewayHeartBeat(InboundMessage inboundMessage) 
        {
            var msg = inboundMessage.Inner as RequestHeartBeat;
            if (msg == null) 
            {
                this.logger.LogError("ProcessGatewayHeartBeat, input message type:{0}", inboundMessage.Inner.GetType());
                return;
            }
            this.messageCenter.SendMessage(new OutboundMessage(inboundMessage.SourceConnection, new ResponseHeartBeat()
            {
                MilliSecond = msg.MilliSecond,
            }));
        }
        private void ProcessGatewayCloseConnection(InboundMessage inboundMessage) 
        {
        }
        private void ProcessGatewaySendMessageToPlayer(InboundMessage inboundMessage) 
        {
        }
    }
}
