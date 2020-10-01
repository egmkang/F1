using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Utils;

namespace F1.Core.Gateway
{
    internal class GatewayClientFactory
    {
        private readonly IPlacement placement;
        private readonly IMessageCenter messageCenter;
        private readonly ILogger logger;
        private readonly IClientConnectionFactory connectionFactory;
        private readonly IMessageHandlerFactory messageHandlerFactory;
        private readonly UniqueSequence uniqueSequence;
        private readonly LRU<long, PlacementActorHostInfo> recentRemovedServer = new LRU<long, PlacementActorHostInfo>(1024);
        //ServerID => IChannel
        private readonly ConcurrentDictionary<long, WeakReference<IChannel>> clients = new ConcurrentDictionary<long, WeakReference<IChannel>>();

        public GatewayClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
            IClientConnectionFactory connectionFactory,
            IMessageHandlerFactory messageHandlerFactory,
            UniqueSequence uniqueSequence
            )
        {
            this.logger = loggerFactory.CreateLogger("F1.Core");
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.connectionFactory = connectionFactory;
            this.messageHandlerFactory = messageHandlerFactory;
            this.uniqueSequence = uniqueSequence;

            //this.placement.RegisterServerChangedEvent(this.OnAddServer, this.OnRemoveServer, this.OnOfflineServer);
            this.logger.LogInformation("GatewayClientFactory Placement RegisterServerChangedEvent");
        }

        private long NewSequenceID => this.uniqueSequence.GetNewSequence();

        public void OnAddServer(PlacementActorHostInfo server) 
        {
        }
        public void OnRemoveServer(PlacementActorHostInfo server) 
        {
        }
        public void OnOfflineServer(PlacementActorHostInfo server) 
        {
        }

        private void TryConnectAsync(PlacementActorHostInfo server) { }
        private void TryCloseConnection(long serverID) { }
    }
}
