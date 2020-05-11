using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using RpcMessage;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Message;
using F1.Core.Utils;
using F1.Core.Network;

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
        private readonly UniqueSequence uniqueSequence;
        private readonly TaskCompletionSourceManager taskCompletionSourceManager;
        private readonly LRU<long, PlacementActorHostInfo> recentRemovedServer = new LRU<long, PlacementActorHostInfo>(1024);
        //ServerID => IChannel
        private readonly ConcurrentDictionary<long, WeakReference<IChannel>> clients = new ConcurrentDictionary<long, WeakReference<IChannel>>();

        public RpcClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
            IClientConnectionFactory connectionFactory,
            IMessageHandlerFactory messageHandlerFactory,
            UniqueSequence uniqueSequence,
            TaskCompletionSourceManager taskCompletionSourceManager) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core");
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.connectionFactory = connectionFactory;
            this.messageHandlerFactory = messageHandlerFactory;
            this.uniqueSequence = uniqueSequence;
            this.taskCompletionSourceManager = taskCompletionSourceManager;

            this.placement.RegisterServerChangedEvent(this.OnAddServer, this.OnRemoveServer, this.OnOfflineServer);
            this.logger.LogInformation("RpcClientFactory Placement RegisterServerChangedEvent");

            this.messageCenter.RegisterMessageProc(typeof(ResponseRpc).FullName, this.ProcessRpcResponse);
            this.messageCenter.RegisterMessageProc(typeof(ResponseRpcHeartBeat).FullName, this.ProcessRpcHeartBeatResponse);
        }

        private long NewSequenceID => this.uniqueSequence.GetNewSequence();

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
            //TODO, 服务器要下线, 要把所有在这个服务器上的玩家清掉
            //但是自己的不能清掉
        }

        const int HeartBeatInterval = 5000;
        private async void TrySendHeatBeat(IChannel channel) 
        {
            var sessionInfo = channel.GetSessionInfo();

            while (sessionInfo.IsActive)
            {
                if (Platform.GetMilliSeconds() - sessionInfo.ActiveTime > 3 * HeartBeatInterval) 
                {
                    this.logger.LogError("HearBeatTimeOut, SessionID:{0}, ServerID:{1}, RemoteAddress:{2}, TimeOut:{3}",
                        sessionInfo.SessionID, sessionInfo.ServerID, sessionInfo.RemoteAddress, Platform.GetMilliSeconds() - sessionInfo.ActiveTime);

                    this.TryCloseCurrentClient(sessionInfo.ServerID);
                    break;
                }

                var msg = new OutboundMessage(channel, new RequestRpcHeartBeat() 
                {
                     MilliSeconds = Platform.GetMilliSeconds(),
                });
                sessionInfo.PutOutboundMessage(msg);

                await Task.Delay(HeartBeatInterval);
            }
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
                        var weak = new WeakReference<IChannel>(channel);
                        this.clients.AddOrUpdate(server.ServerID, weak, (_1, _2) => weak);

                        this.TrySendHeatBeat(channel);
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
                if (this.clients.TryGetValue(serverID, out var c) && c.TryGetTarget(out var channel))
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

        public async Task<ResponseRpc> TrySendRpcRequest(PlacementFindActorPositionRequest actor,
                                                        object message,
                                                        bool needClearPosition) 
        {
            if (needClearPosition) this.placement.ClearActorPositionCache(actor);
            for (int i = 0; i < 2; ++i) 
            {
                try
                {
                    var position = await this.placement.FindActorPositonAsync(actor);
                    if (position != null) 
                    {
                        if (this.logger.IsEnabled(LogLevel.Trace)) 
                        {
                            this.logger.LogTrace("FindActorPosition, Actor:{0}@{1}, Position:{2}",
                                actor.ActorType, actor.ActorID, position.ServerID);
                        }
                        var server = this.GetChannelByServerID(position.ServerID);
                        if (server != null) 
                        {
                            return await this.SendRpcMessage(server, message);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(PlacementException)) 
                    {
                        await Task.Delay(1000);
                    }
                    this.logger.LogError("TrySendRpcRequest, Actor:{0}@{1}, Exception:{2}",
                        actor.ActorType, actor.ActorID, e.Message);
                }
            }

            throw new Exception("TrySendRpcRequest more then 2 times");
        }

        public IChannel GetChannelByServerID(long serverID) 
        {
            if (this.clients.TryGetValue(serverID, out var c) && c.TryGetTarget(out var channel)) 
            {
                return channel;
            }
            return null;
        }

        public Task<ResponseRpc> SendRpcMessage(IChannel channel, object message)
        {
            var request = message as RequestRpc;
            Contract.Assert(request != null);
            Contract.Assert(channel != null);

            request.ResponseId = this.NewSequenceID;
            request.DestServerId = channel.GetSessionInfo().ServerID;

            var outboundMessage = new OutboundMessage(channel, message);
            this.messageCenter.SendMessage(outboundMessage);
            if (this.logger.IsEnabled(LogLevel.Trace)) 
            {
                this.logger.LogTrace("SendRpcMessage, {0}", message.GetType());
            }

            if (!request.NeedResult) 
            {
                return Task.FromResult(new ResponseRpc());
            }

            var completionSource = new GenericCompletionSource<ResponseRpc>();
            completionSource.ID = request.ResponseId;
            this.taskCompletionSourceManager.Push(completionSource);
            return completionSource.GetTask() as Task<ResponseRpc>;
        }

        private void ProcessRpcResponseError(ResponseRpc msg, IGenericCompletionSource completionSource) 
        {
            if (msg.ErrorCode == (int)RpcErrorCode.ActorHasNewPosition)
            {
                var actorInfo = new PlacementFindActorPositionRequest()
                {
                    Domain = "t",
                    ActorID = msg.Request.ActorId,
                    ActorType = msg.Request.ActorType,
                    TTL = 0,
                };

                //这边要看错误是否可以通过重试解决
                _ = this.TrySendRpcRequest(actorInfo, msg, true);
            }
            else if (msg.ErrorCode == (int)RpcErrorCode.MethodNotFound)
            {
                completionSource.WithException(new RpcDispatchException("Method Not Found"));
            }
            else 
            {
                completionSource.WithException(new Exception(msg.ErrorMsg));
            }
        }

        private void ProcessRpcHeartBeatResponse(InboundMessage message) 
        {
            var msg = message.Inner as ResponseRpcHeartBeat;
            if (msg == null) 
            {
                this.logger.LogError("ProcessRpcHeartBeat input message type:{0}", message.GetType());
                return;
            }
            var elapsedTime = Platform.GetMilliSeconds() - msg.MilliSeconds;
            if (elapsedTime > 100) 
            {
                var sessionInfo = message.SourceConnection.GetSessionInfo();
                this.logger.LogWarning("ProcessRpcHearBeat, SessionID:{0}, ServerID:{1}, RemoteAddress:{2}, Elapsed Time:{3}ms",
                    sessionInfo.SessionID, sessionInfo.ServerID, sessionInfo.RemoteAddress, elapsedTime);
            }
        }

        private void ProcessRpcResponse(InboundMessage message)
        {
            var msg = message.Inner as ResponseRpc;
            if (msg == null)
            {
                this.logger.LogError("ProcessRpcResponse input message type:{0}", message.Inner.GetType());
                return;
            }

            var completionSource = this.taskCompletionSourceManager.GetCompletionSource(msg.ResponseId);
            if (completionSource == null) 
            {
                this.logger.LogWarning("ProcessRpcResponse fail, ResponseID:{0}", msg.ResponseId);
                return;
            }

            if (msg.ErrorCode != 0) 
            {
                this.ProcessRpcResponseError(msg, completionSource);
                return;
            }

            completionSource.WithResult(msg);
        }
    }
}
