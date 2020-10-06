using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Network;
using F1.Abstractions.Actor.Gateway;
using F1.Abstractions.Placement;
using F1.Core.Message;
using F1.Core.Network;
using GatewayMessage;

namespace F1.Gateway
{
    internal class GatewayDefaultMessageHandler
    {
        public readonly ILogger logger;
        public readonly IMessageCenter messageCenter;
        public readonly IConnectionManager connectionManager;
        public readonly IAuthentication authentication;
        public readonly IPlacement placement;

        public GatewayDefaultMessageHandler(ILoggerFactory loggerFactory,
                                        IMessageCenter messageCenter,
                                        IConnectionManager connectionManager,
                                        IAuthentication authentication,
                                        IPlacement placement) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Gateway");
            this.messageCenter = messageCenter;
            this.connectionManager = connectionManager;
            this.authentication = authentication;
            this.placement = placement;

            this.messageCenter.RegisterMessageProc(BlockMessageCodec.MessageName, GatewayIncommingMessage, false);

            this.messageCenter.RegisterTypedMessageProc<RequestHeartBeat>(ProcessGatewayHeartBeat);
            this.messageCenter.RegisterTypedMessageProc<RequestCloseConnection>(ProcessGatewayCloseConnection);
            this.messageCenter.RegisterTypedMessageProc<RequestSendMessageToPlayer>(ProcessGatewaySendMessageToPlayer);
        }

        private void GatewayIncommingMessage(InboundMessage inboundMessage)
        {
            var sessionInfo = inboundMessage.SourceConnection.GetSessionInfo();
            var playerInfo = sessionInfo.GetPlayerInfo();

            //第一个消息
            if (string.IsNullOrEmpty(playerInfo.PlayerID))
            {
                var playerID = this.authentication.DecodeToken(inboundMessage.Inner as byte[]) as string;
                logger.LogInformation("GatewayIncomingMessage, SessionID:{0} FirstMessage, PlayerID:{1}",
                    sessionInfo.SessionID, playerID);
                playerInfo.PlayerID = playerID;


            }
            else 
            {

            }
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
            var msg = inboundMessage.Inner as RequestCloseConnection;
            if (msg == null) 
            {
                this.logger.LogError("ProcessGatewayCloseConnection, input message type:{0}", inboundMessage.Inner.GetType());
                return;
            }
            var channel = this.connectionManager.GetConnection(msg.SessionId);
            if (channel != null)
            {
                try
                {
                   _ = channel.CloseAsync();
                }
                catch { }
                this.logger.LogInformation("ProcessGatewayCloseConnection, SessionID:{0} closed", msg.SessionId);
            }
            else 
            {
                this.logger.LogWarning("ProcessGatewayCloseConnection, SessionID:{0} not found", msg.SessionId);
            }
        }

        private void ProcessGatewaySendMessageToPlayer(InboundMessage inboundMessage) 
        {
            var msg = inboundMessage.Inner as RequestSendMessageToPlayer;
            if (msg == null) 
            {
                this.logger.LogError("ProcessGatewaySendMessageToPlayer, input message type:{0}", inboundMessage.Inner.GetType());
                return;
            }
            //TODO
            var bytes = msg.Msg.ToByteArray();
            foreach (var sessionId in msg.SessionIds) 
            {
                var channel = this.connectionManager.GetConnection(sessionId);
                if (channel != null)
                {
                    this.messageCenter.SendMessage(new OutboundMessage(channel, bytes));
                }
                else 
                {
                    this.logger.LogWarning("ProcessGatewaySendMessageToPlayer, SessionID:{0} not found", sessionId);
                }
            }
        }
    }
}
