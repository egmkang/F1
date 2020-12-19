using System;
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
    internal class GatewayClientFactory
    {
        private readonly IPlacement placement;
        private readonly IMessageCenter messageCenter;
        private readonly ILogger logger;
        private readonly ClientConnectionPool clientConnectionPool;

        public GatewayClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
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
            if (elapsedTime >= 5)
            {
                var sessionInfo = message.SourceConnection.GetSessionInfo();
                this.logger.LogWarning("ProcessHearBeat, SessionID:{0}, ServerID:{1}, RemoteAddress:{2}, Elapsed Time:{3}ms",
                    sessionInfo.SessionID, sessionInfo.ServerID, sessionInfo.RemoteAddress, elapsedTime);
            }
        }

        private void ProcessNotifyConnectionComing(InboundMessage message) 
        {
            var msg = message.Inner as NotifyConnectionComing;
            if (msg == null) 
            {
                this.logger.LogError("ProcessNotifyConnectionComing input message type:{0}", message.Inner?.GetType());
                return;
            }
            this.messageCenter.OnReceiveUserMessage(msg.ServiceType, msg.ActorId, message);
        }

        private void ProcessNotifyConnectionAborted(InboundMessage message)
        {
            var msg = message.Inner as NotifyConnectionAborted;
            if (msg == null)
            {
                this.logger.LogError("ProcessNotifyConnectionAborted input message type:{0}", message.Inner?.GetType());
                return;
            }
            var actorID = msg.ActorId;
            if (string.IsNullOrEmpty(actorID)) 
            {
                this.logger.LogWarning("ProcessNotifyConnectionAborted, SessionID:{0}, ActorID not found", msg.SessionId);
                return;
            } 
            this.messageCenter.OnReceiveUserMessage(msg.ServiceType, actorID, message);

            Task.Run(async () =>
            {
                //1分钟后GC掉SessionInfo
                await Task.Delay(60 * 1000).ConfigureAwait(false);
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
            var actorID = msg.ActorId;
            if (string.IsNullOrEmpty(actorID)) 
            {
                this.logger.LogWarning("ProcessNewMessage, SessionID:{0}, ActorID not found", msg.SessionId);
                return;
            }
            if (msg.Trace.Length != 0) 
            {
                this.logger.LogWarning("PrcessNewMessage, SessionID:{0}, actorID:{1}, Trace:{2}",
                    msg.SessionId, actorID, msg.Trace);
            }
            this.messageCenter.OnReceiveUserMessage(msg.ServiceType, actorID, message);
        }

    }
}
