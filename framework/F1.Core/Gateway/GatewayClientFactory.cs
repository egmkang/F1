﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Utils;
using F1.Core.Network;
using GatewayMessage;
using F1.Core.Message;

namespace F1.Core.Gateway
{
    internal class GatewayPlayerInfo 
    {
        public long SessionID;
        public string PlayerID;
    }
    internal class GatewayClientFactory
    {
        private const int SessionInfosCount = 1024 * 100;
        private readonly IPlacement placement;
        private readonly IMessageCenter messageCenter;
        private readonly ILogger logger;
        private readonly ClientConnectionPool clientConnectionPool;
        private readonly LRU<long, GatewayPlayerInfo> sessionInfos = new LRU<long, GatewayPlayerInfo>(SessionInfosCount);

        public GatewayClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
            IClientConnectionFactory connectionFactory,
            IMessageHandlerFactory messageHandlerFactory,
            ClientConnectionPool clientPool
            )
        {
            this.logger = loggerFactory.CreateLogger("F1.Core");
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.clientConnectionPool = clientPool;

            //消息处理
            this.messageCenter.RegisterTypedMessageProc<ResponseHeartBeat>(this.ProcessHeartBeatResponse);
            this.messageCenter.RegisterTypedMessageProc<NotifyConnectionComing>(this.ProcessNotifyConnectionComing);
            this.messageCenter.RegisterTypedMessageProc<NotifyConnectionAborted>(this.ProcessNotifyConnectionAborted);
            this.messageCenter.RegisterTypedMessageProc<NotifyNewMessage>(this.ProcessNotifyNewMessage);
        }

        public void OnAddServer(PlacementActorHostInfo server)
        {
            this.clientConnectionPool.OnAddServer(server.ServerID,
                IPEndPoint.Parse(server.Address),
                () => new RequestHeartBeat() { MilliSecond = Platform.GetMilliSeconds(), }
            );
        }
        public void OnRemoveServer(PlacementActorHostInfo server)
        {
            this.clientConnectionPool.OnRemoveServer(server.ServerID);
        }
        public void OnOfflineServer(PlacementActorHostInfo server)
        {
            //TODO
            //貌似不需要干什么
        }

        private void ProcessHeartBeatResponse(InboundMessage message)
        {
            var msg = message.Inner as ResponseHeartBeat;
            if (msg == null)
            {
                this.logger.LogError("ProcessHeartBeat input message type:{0}", message.Inner?.GetType());
                return;
            }
            var elapsedTime = Platform.GetMilliSeconds() - msg.MilliSecond;
            if (elapsedTime > 1)
            {
                var sessionInfo = message.SourceConnection.GetSessionInfo();
                this.logger.LogWarning("ProcessHearBeat, SessionID:{0}, ServerID:{1}, RemoteAddress:{2}, Elapsed Time:{3}ms",
                    sessionInfo.SessionID, sessionInfo.ServerID, sessionInfo.RemoteAddress, elapsedTime);
            }
        }

        private void UpdateSessionInfo(NotifyConnectionComing message)
        {
            //这边要把SessionID对应的OpenID, PlayerID记住
            //还要考虑GC, 暂时用LRU来GC
            this.sessionInfos.Add(message.SessionId, new GatewayPlayerInfo()
            {
                PlayerID = message.PlayerId,
                SessionID = message.SessionId,
            });
        }

        private void RemoveSessionInfo(long sessionID)
        {
            this.sessionInfos.Remove(sessionID);
        }

        private string GetSessionInfo(long sessionID)
        {
            var session = this.sessionInfos.Get(sessionID);
            if (session != null) 
            {
                return session.PlayerID;
            }
            return "";
        }

        private void ProcessNotifyConnectionComing(InboundMessage message) 
        {
            var msg = message.Inner as NotifyConnectionComing;
            if (msg == null) 
            {
                this.logger.LogError("ProcessNotifyConnectionComing input message type:{0}", message.Inner?.GetType());
                return;
            }
            this.UpdateSessionInfo(msg);
            this.messageCenter.OnReceiveUserMessage(msg.ServiceType, msg.PlayerId.ToString(), message);
        }

        private void ProcessNotifyConnectionAborted(InboundMessage message)
        {
            var msg = message.Inner as NotifyConnectionAborted;
            if (msg == null)
            {
                this.logger.LogError("ProcessNotifyConnectionAborted input message type:{0}", message.Inner?.GetType());
                return;
            }
            var playerID = this.GetSessionInfo(msg.SessionId);
            if (string.IsNullOrEmpty(playerID)) 
            {
                this.logger.LogWarning("ProcessNotifyConnectionAborted, SessionID:{0}, PlayerID not found", msg.SessionId);
                return;
            } 
            this.messageCenter.OnReceiveUserMessage(msg.ServiceType, playerID, message);
            Task.Run(async () =>
            {
                //1分钟后GC掉
                await Task.Delay(60 * 1000).ConfigureAwait(false);
                this.RemoveSessionInfo(msg.SessionId);
            });
        }

        private void ProcessNotifyNewMessage(InboundMessage message) 
        {
            var msg = message.Inner as NotifyNewMessage;
            if (msg == null) 
            {
                this.logger.LogError("ProcessNewMessage input message type:{0}", message.Inner?.GetType());
                return;
            }
            var playerID = this.GetSessionInfo(msg.SessionId);
            if (string.IsNullOrEmpty(playerID)) 
            {
                this.logger.LogWarning("ProcessNewMessage, SessionID:{0}, PlayerID not found", msg.SessionId);
                return;
            }
            this.messageCenter.OnReceiveUserMessage(msg.ServiceType, playerID, message);
        }

    }
}
