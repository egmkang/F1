using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Network;
using F1.Core.Message;
using F1.Abstractions.Placement;
using F1.Core.Utils;
using F1.Core.Network;
using RpcMessage;

namespace F1.Core.RPC
{
    internal class RpcClientFactory
    {
        private const int MaxRetryInterval = 5;
        private readonly IPlacement placement;
        private readonly IMessageCenter messageCenter;
        private readonly ILogger logger;
        private readonly IClientConnectionFactory connectionFactory;
        private readonly IMessageHandlerFactory messageHandlerFactory;
        private readonly LRU<long, PlacementActorHostInfo> recentRemovedServer = new LRU<long, PlacementActorHostInfo>(1024);
        private readonly ConcurrentDictionary<long, IChannel> clients = new ConcurrentDictionary<long, IChannel>();

        public RpcClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
            IClientConnectionFactory connectionFactory,
            IMessageHandlerFactory messageHandlerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core");
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.connectionFactory = connectionFactory;
            this.messageHandlerFactory = messageHandlerFactory;

            this.placement.RegisterServerChangedEvent(this.OnAddServer, this.OnRemoveServer, this.OnOfflineServer);
            this.logger.LogInformation("RpcClientFactory Placement RegisterServerChangedEvent");
        }

        private void OnAddServer(PlacementActorHostInfo server) 
        {
            this.TryConnectAsync(server);
        }

        private void OnRemoveServer(PlacementActorHostInfo server) 
        {
            this.recentRemovedServer.Add(server.ServerID, server);
            this.TryCloseCurrentClient(server.ServerID);
        }
        private void OnOfflineServer(PlacementActorHostInfo server)
        {
        }

        private void TryConnectAsync(PlacementActorHostInfo server)
        {
            Task.Run(async () =>
            {
                this.logger.LogInformation("TryConnectAsync, ServerID:{0}, Address:{1} Start", server.ServerID, server.Address);

                var reteyCount = 0;
                while (true)
                {
                    try
                    {
                        if (this.recentRemovedServer.Get(server.ServerID) != null) 
                        {
                            this.logger.LogInformation("TryConnectAsync, ServerID:{0} has been canceled", server.ServerID);
                            break;
                        }
                        var channel = await this.connectionFactory.ConnectAsync(IPEndPoint.Parse(server.Address), this.messageHandlerFactory);
                        var sessionInfo = channel.GetSessionInfo();
                        this.logger.LogInformation("TryConnectAsync, ServerID:{0}, Address:{1}, SessionID:{2}",
                            server.ServerID, server.Address, sessionInfo.SessionID);

                        sessionInfo.ServerID = server.ServerID;
                        this.clients.AddOrUpdate(server.ServerID, channel, (_1, _2) => channel);
                        break;
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError("TryConnectAsync, ServerID:{0}, Address:{1}, Exception:{2}",
                            server.ServerID, server.Address, e.Message);
                    }
                    if (reteyCount >= MaxRetryInterval) reteyCount = MaxRetryInterval;
                    await Task.Delay(reteyCount * 1000);
                    reteyCount++;
                }
            });
        }

        private void TryCloseCurrentClient(long serverID) 
        {
            try
            {
                if (this.clients.TryGetValue(serverID, out var channel))
                {
                    this.logger.LogInformation("TryCloseCurrentClient, ServerID:{1} SessionID:{0}",
                        channel.GetSessionInfo().SessionID, serverID);
                    channel.CloseAsync();

                    this.clients.TryRemove(serverID, out var _);
                }
                else 
                {
                    this.logger.LogInformation("TryCloseCurrentClient, cannot find ServerID:{0}", serverID);
                }
            }
            catch (Exception e)
            {
                logger.LogError("TryCloseCurrentClient, Exception:{0}", e.Message);
            }
        }

        public WeakReference<IChannel> GetChannelByServerID(long serverID) 
        {
            if (this.clients.TryGetValue(serverID, out var channel)) 
            {
                return new WeakReference<IChannel>(channel);
            }
            return null;
        }

        public bool SendRpcRequest(long serverID, RequestRpc request)
        {
            if (this.clients.TryGetValue(serverID, out var channel))
            {
                var outboundMessage = new OutboundMessage(channel, request);
                this.messageCenter.SendMessage(outboundMessage);
                return true;
            }
            return false;
        }
    }
}
