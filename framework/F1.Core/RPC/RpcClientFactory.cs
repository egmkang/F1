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
using F1.Core.Gateway;

namespace F1.Core.RPC
{
    internal class RpcClientFactory
    {
        private readonly IPlacement placement;
        private readonly IMessageCenter messageCenter;
        private readonly ILogger logger;
        private readonly UniqueSequence uniqueSequence;
        private readonly TaskCompletionSourceManager taskCompletionSourceManager;
        private readonly GatewayClientFactory gatewayClientFactory;
        private readonly ClientConnectionPool clientConnectionPool;


        public RpcClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
            UniqueSequence uniqueSequence,
            TaskCompletionSourceManager taskCompletionSourceManager,
            GatewayClientFactory gatewayClientFactory,
            ClientConnectionPool clientConnectionPool) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core");
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.uniqueSequence = uniqueSequence;
            this.taskCompletionSourceManager = taskCompletionSourceManager;
            this.gatewayClientFactory = gatewayClientFactory;
            this.clientConnectionPool = clientConnectionPool;

            this.placement.RegisterServerChangedEvent(this.OnAddServer, this.OnRemoveServer, this.OnOfflineServer);
            this.logger.LogInformation("RpcClientFactory Placement RegisterServerChangedEvent");

            this.messageCenter.RegisterMessageProc(typeof(ResponseRpc).FullName, this.ProcessRpcResponse);
            this.messageCenter.RegisterMessageProc(typeof(ResponseRpcHeartBeat).FullName, this.ProcessRpcHeartBeatResponse);
        }

        private long NewSequenceID => this.uniqueSequence.GetNewSequence();

        private bool IsGateway(PlacementActorHostInfo server) => server.ActorType.Count == 1 && server.ActorType[0] == GatewayConstant.ServiceGateway;

        private void OnAddServer(PlacementActorHostInfo server) 
        {
            if (IsGateway(server)) 
            {
                this.gatewayClientFactory.OnAddServer(server);
                return;
            }
            this.clientConnectionPool.OnAddServer(server.ServerID,
                IPEndPoint.Parse(server.Address),
                () => new RequestRpcHeartBeat() { MilliSeconds = Platform.GetMilliSeconds() });
        }

        private void OnRemoveServer(PlacementActorHostInfo server) 
        {
            if (IsGateway(server)) 
            {
                this.gatewayClientFactory.OnRemoveServer(server);
                return;
            }
            this.clientConnectionPool.OnRemoveServer(server.ServerID);
        }
        private void OnOfflineServer(PlacementActorHostInfo server)
        {
            if (IsGateway(server)) 
            {
                this.gatewayClientFactory.OnOfflineServer(server);
                return;
            }
            //这边通过每次获取Actor位置的时候判断服务器是否下线来做的
            //所以暂时不需要处理
            //服务器要下线, 要把所有在这个服务器上的玩家清掉
            //但是自己的不能清掉
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
                                actor.ActorImplType, actor.ActorID, position.ServerID);
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
                        actor.ActorImplType, actor.ActorID, e.Message);
                }
            }

            throw new Exception("TrySendRpcRequest more then 2 times");
        }

        public IChannel GetChannelByServerID(long serverID) 
        {
            return this.clientConnectionPool.GetChannelByServerID(serverID);
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
                    ActorID = msg.Request.ActorId,
                    ActorImplType = msg.Request.ActorType,
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
            if (elapsedTime > 1) 
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
