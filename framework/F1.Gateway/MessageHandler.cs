using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Google.Protobuf;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Abstractions.Actor.Gateway;
using F1.Abstractions.Placement;
using F1.Core.Message;
using F1.Core.Network;
using F1.Core.Placement;
using GatewayMessage;

namespace F1.Gateway
{
    internal class GatewayDefaultMessageHandler
    {
        public readonly ILogger logger;
        public readonly IMessageCenter messageCenter;
        public readonly IConnectionManager connectionManager;
        public readonly IAuthentication authentication;
        public readonly PlacementExtension placement;

        public string ServiceTypeName { get; internal set; } = "IPlayer";

        public GatewayDefaultMessageHandler(ILoggerFactory loggerFactory,
                                        IMessageCenter messageCenter,
                                        IConnectionManager connectionManager,
                                        IAuthentication authentication,
                                        PlacementExtension placement) 
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
            var data = inboundMessage.Inner as byte[];

            //第一个消息
            if (string.IsNullOrEmpty(playerInfo.PlayerID))
            {
                var playerID = this.authentication.DecodeToken(data) as string;
                logger.LogInformation("GatewayIncomingMessage, SessionID:{0} FirstMessage, PlayerID:{1}",
                    sessionInfo.SessionID, playerID);
                playerInfo.PlayerID = playerID;
                _ = this.ProcessGatewayMessageSlow(inboundMessage.SourceConnection, sessionInfo, playerID, data, true);
            }
            else 
            {
                if (sessionInfo.ServerID != 0)
                {
                    var result = this.messageCenter.SendMessageToServer(sessionInfo.ServerID, new NotifyNewMessage()
                    {
                        Msg = ByteString.CopyFrom(data),
                        ServiceType = this.ServiceTypeName,
                        SessionId = sessionInfo.SessionID,
                    });
                    if (result) return;
                }
                _ = this.ProcessGatewayMessageSlow(inboundMessage.SourceConnection, sessionInfo, playerInfo.PlayerID, data, false);
            }
        }

        private async Task ProcessGatewayMessageSlow(IChannel channel,
                                                    IConnectionSessionInfo sessionInfo, 
                                                    string playerID, 
                                                    byte[] data, 
                                                    bool isFirstPacket)
        {
            var serviceType = this.ServiceTypeName;
            try
            {
                var position = await this.placement.FindActorPositonAsync(serviceType, playerID).ConfigureAwait(false);
                if (position == null) 
                {
                    this.logger.LogError("ProcessGatewayMessageSlow, SessionID:{0}, PlayerID:{1} not found DestServer",
                        sessionInfo.SessionID, playerID);
                    return;
                }
                sessionInfo.ServerID = position.ServerID;

                if (isFirstPacket)
                {

                    this.logger.LogInformation("IncomingConnection, SessionID:{0}, PlayerID:{1}, DestServerID:{2}",
                        sessionInfo.SessionID, playerID, position.ServerID);

                    var msg = new NotifyConnectionComing()
                    {
                        Token = ByteString.CopyFrom(data),
                        SessionId = sessionInfo.SessionID,
                        PlayerId = playerID,
                        ServiceType = serviceType,
                    };
                    this.messageCenter.SendMessageToServer(position.ServerID, msg);
                }
                else
                {
                    this.logger.LogInformation("NotifyNewMessage, SessionID:{0}, PlayerID:{1}, DestServerID:{2}",
                        sessionInfo.SessionID, playerID, position.ServerID);

                    var msg = new NotifyNewMessage()
                    {
                        Msg = ByteString.CopyFrom(data),
                        SessionId = sessionInfo.SessionID,
                        ServiceType = serviceType,
                    };
                    this.messageCenter.SendMessageToServer(position.ServerID, msg);
                }
            }
            catch (Exception e)
            {
                this.logger.LogError("ProcessGatewayMessageSlow, SessionID:{0}, PlayerID:{1}, Exception:{2}",
                    sessionInfo.SessionID, playerID, e);
            }
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
