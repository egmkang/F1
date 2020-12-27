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
using F1.Core.Utils;
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

        public string ServiceTypeName { get; internal set; } = "IAccount";

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
            this.messageCenter.RegisterTypedMessageProc<RequestChangeMessageDestination>(ProcessGatewayChangeMessageDestination);
        }

        private void GatewayIncommingMessage(InboundMessage inboundMessage)
        {
            var sessionInfo = inboundMessage.SourceConnection.GetSessionInfo();
            var playerInfo = sessionInfo.GetPlayerInfo();
            var data = inboundMessage.Inner as byte[];

            //第一个消息
            if (string.IsNullOrEmpty(playerInfo.AccountID))
            {
                var accountID = this.authentication.DecodeToken(data) as string;
                logger.LogInformation("GatewayIncomingMessage, SessionID:{0} FirstMessage, Actor:{1}/{2}",
                    sessionInfo.SessionID, this.ServiceTypeName, accountID);
                playerInfo.AccountID = accountID;
                _ = this.ProcessGatewayMessageSlow(inboundMessage.SourceConnection, sessionInfo, playerInfo, data, true);
            }
            else
            {
                if (sessionInfo.ServerID != 0)
                {
                    var result = this.messageCenter.SendMessageToServer(sessionInfo.ServerID, new NotifyNewMessage()
                    {
                        Msg = ByteString.CopyFrom(data),
                        ServiceType =  playerInfo.DestServiceType,
                        ActorId = playerInfo.DestActorID,
                        SessionId = sessionInfo.SessionID,
                    }.AsRpcMessage());
                    if (result) return;
                }
                _ = this.ProcessGatewayMessageSlow(inboundMessage.SourceConnection, sessionInfo, playerInfo, data, false);
            }
        }

        private async Task ProcessGatewayMessageSlow(IChannel channel,
                                                    IConnectionSessionInfo sessionInfo,
                                                    GatewayPlayerSessionInfo gatewayPlayer,
                                                    byte[] data,
                                                    bool isFirstPacket)
        {
            if (string.IsNullOrEmpty(gatewayPlayer.DestServiceType)) 
            {
                gatewayPlayer.DestServiceType = this.ServiceTypeName;
                this.logger.LogInformation("ProcessGatewatMessageSlow, ChangeDestServiceType:{0}", gatewayPlayer.DestServiceType);
            }
            if (string.IsNullOrEmpty(gatewayPlayer.DestActorID)) 
            {
                gatewayPlayer.DestActorID = gatewayPlayer.AccountID;
            }
            try
            {
                var position = await this.placement.FindActorPositonAsync(gatewayPlayer.DestServiceType, gatewayPlayer.DestActorID).ConfigureAwait(false);
                if (position == null)
                {
                    this.logger.LogError("ProcessGatewayMessageSlow, SessionID:{0}, Actor:{1}/{2} not found DestServer",
                        sessionInfo.SessionID, gatewayPlayer.DestServiceType, gatewayPlayer.DestActorID);
                    return;
                }
                sessionInfo.ServerID = position.ServerID;

                if (isFirstPacket)
                {

                    this.logger.LogInformation("IncomingConnection, SessionID:{0}, Actor:{1}/{2}, DestServerID:{3}",
                        sessionInfo.SessionID, gatewayPlayer.DestServiceType, gatewayPlayer.DestActorID, position.ServerID);

                    var msg = new NotifyConnectionComing()
                    {
                        Token = ByteString.CopyFrom(data),
                        SessionId = sessionInfo.SessionID,
                        ActorId = gatewayPlayer.DestActorID,
                        ServiceType = gatewayPlayer.DestServiceType,
                    };
                    this.messageCenter.SendMessageToServer(position.ServerID, msg.AsRpcMessage());
                }
                else
                {
                    this.logger.LogInformation("NotifyNewMessage, SessionID:{0}, Actor:{1}/{2}, DestServerID:{3}",
                        sessionInfo.SessionID, gatewayPlayer.DestServiceType, gatewayPlayer.DestActorID, position.ServerID);

                    var msg = new NotifyNewMessage()
                    {
                        Msg = ByteString.CopyFrom(data),
                        SessionId = sessionInfo.SessionID,
                        ServiceType = gatewayPlayer.DestServiceType,
                        ActorId = gatewayPlayer.DestActorID,
                    };
                    this.messageCenter.SendMessageToServer(position.ServerID, msg.AsRpcMessage());
                }
            }
            catch (Exception e)
            {
                this.logger.LogError("ProcessGatewayMessageSlow, SessionID:{0}, Actor:{1}/{2}, Exception:{3}",
                    sessionInfo.SessionID, gatewayPlayer.DestServiceType, gatewayPlayer.DestActorID, e);
            }
        }

        private void ProcessGatewayHeartBeat(InboundMessage inboundMessage)
        {
            var msg = (inboundMessage.Inner as RpcMessage).Meta as RequestHeartBeat;
            if (msg == null)
            {
                this.logger.LogError("ProcessGatewayHeartBeat, input message type:{0}", inboundMessage.Inner.GetType());
                return;
            }
            var resp = new ResponseHeartBeat() 
            { 
                MilliSecond = msg.MilliSecond,
            };
            this.messageCenter.SendMessage(new OutboundMessage(inboundMessage.SourceConnection, resp.AsRpcMessage()));
        }
        private void ProcessGatewayCloseConnection(InboundMessage inboundMessage)
        {
            var msg = (inboundMessage.Inner as  RpcMessage).Meta as RequestCloseConnection;
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

        private void ProcessGatewayChangeMessageDestination(InboundMessage inboundMessage) 
        {
            var msg = (inboundMessage.Inner as RpcMessage).Meta as RequestChangeMessageDestination;
            if (msg == null) 
            {
                this.logger.LogError("ProcessGatewayChangeMessageDestination, input message type:{0}", inboundMessage.Inner.GetType());
                return;
            }

            var channel = this.connectionManager.GetConnection(msg.SessionId);
            if (channel == null) 
            {
                this.logger.LogInformation("ProcessGatewayChangeMessageDestination, SessionID:{0} not found", msg.SessionId);
                return;
            }
            var sessionInfo = channel.GetSessionInfo();
            var playerInfo = sessionInfo.GetPlayerInfo();

            playerInfo.DestServiceType = msg.NewServiceType;
            playerInfo.DestActorID = msg.NewActorId;
            sessionInfo.ServerID = 0;

            this.logger.LogInformation("ProcessGatewayChangeMessageDestination, SessionID:{0}, Account:{1}, NewActor:{2}/{3}",
                sessionInfo.SessionID, playerInfo.AccountID, playerInfo.DestServiceType, playerInfo.DestActorID);
        }

        private void ProcessGatewaySendMessageToPlayer(InboundMessage inboundMessage) 
        {
            var msg = (inboundMessage.Inner as RpcMessage).Meta as RequestSendMessageToPlayer;
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
                if (msg.Trace.Length != 0)
                {
                    this.logger.LogInformation("ProcessGatewaySendMessageToPlayer, SessionID:{0}, Trace:{1}", sessionId, msg.Trace);
                }
            }
        }
    }
}
