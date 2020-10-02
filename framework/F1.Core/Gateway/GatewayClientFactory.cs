using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Utils;
using F1.Core.Network;
using GatewayMessage;

namespace F1.Core.Gateway
{
    internal class GatewayClientFactory
    {
        private readonly IPlacement placement;
        private readonly IMessageCenter messageCenter;
        private readonly ILogger logger;
        private readonly UniqueSequence uniqueSequence;
        private readonly ClientConnectionPool clientConnectionPool;

        public GatewayClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
            IClientConnectionFactory connectionFactory,
            IMessageHandlerFactory messageHandlerFactory,
            UniqueSequence uniqueSequence,
            ClientConnectionPool clientPool
            )
        {
            this.logger = loggerFactory.CreateLogger("F1.Core");
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.uniqueSequence = uniqueSequence;
            this.clientConnectionPool = clientPool;

            //TODO:
            //消息处理
            //this.placement.RegisterServerChangedEvent(this.OnAddServer, this.OnRemoveServer, this.OnOfflineServer);
            this.logger.LogInformation("GatewayClientFactory Placement RegisterServerChangedEvent");
        }

        private long NewSequenceID => this.uniqueSequence.GetNewSequence();

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
    }
}
