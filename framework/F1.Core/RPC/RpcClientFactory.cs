using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Message;
using F1.Core.Utils;
using F1.Core.Network;
using F1.Core.Gateway;
using F1.Abstractions.Abstractions.Gateway;
using Rpc;

namespace F1.Core.RPC
{
    internal class RpcClientFactory
    {
        private readonly IPlacement placement;
        private readonly IMessageCenter messageCenter;
        private readonly ILogger logger;
        private readonly TaskCompletionSourceManager taskCompletionSourceManager;
        private readonly GatewayClientFactory gatewayClientFactory;
        private readonly ClientConnectionPool clientConnectionPool;
        private readonly TimeBasedSequence timeBasedSequence;


        public RpcClientFactory(ILoggerFactory loggerFactory,
            IMessageCenter messageCenter,
            IPlacement placement,
            TimeBasedSequence timeBasedSequence,
            TaskCompletionSourceManager taskCompletionSourceManager,
            ClientConnectionPool clientConnectionPool,
            IServiceProvider serviceProvider) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core");
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.timeBasedSequence = timeBasedSequence;
            this.taskCompletionSourceManager = taskCompletionSourceManager;
            this.gatewayClientFactory = serviceProvider.GetService<GatewayClientFactory>();
            this.clientConnectionPool = clientConnectionPool;

            this.placement.RegisterServerChangedEvent(this.OnAddServer, this.OnRemoveServer, this.OnOfflineServer);
            this.logger.LogInformation("RpcClientFactory Placement RegisterServerChangedEvent");

            this.messageCenter.RegisterTypedMessageProc<RpcResponse>(this.ProcessRpcResponse);
            this.messageCenter.RegisterTypedMessageProc<RpcHeartBeatResponse>(this.ProcessRpcHeartBeatResponse);
        }

        private long NewSequenceID => this.timeBasedSequence.GetNewSequence();
        /// <summary>
        /// Gateway在PD里面也是一个ActorHost, 只是提供的服务名为`IGateway`, 进行了特殊处理
        /// </summary>
        public static readonly string ServiceGateway = typeof(IGateway).Name;

        private bool IsGateway(PlacementActorHostInfo server) => server.Services.Count == 1 && server.Services.ContainsKey(ServiceGateway);

        private void OnAddServer(PlacementActorHostInfo server) 
        {
            if (IsGateway(server)) 
            {
                this.gatewayClientFactory?.OnAddServer(server);
                return;
            }
            Func<object> fn = () =>
            {
                var rpcMessage = new RpcMessage() 
                {
                    Meta = new RpcHeartBeatRequest() { RequestMilliseconds = Platform.GetMilliSeconds() },
                };
                return rpcMessage;
            };
            this.clientConnectionPool.OnAddServer(server.ServerID,
                                            IPEndPoint.Parse(server.Address),
                                            fn);
        }

        private void OnRemoveServer(PlacementActorHostInfo server) 
        {
            if (IsGateway(server)) 
            {
                this.gatewayClientFactory?.OnRemoveServer(server);
                return;
            }
            this.clientConnectionPool.OnRemoveServer(server.ServerID);
        }
        private void OnOfflineServer(PlacementActorHostInfo server)
        {
            if (IsGateway(server)) 
            {
                this.gatewayClientFactory?.OnOfflineServer(server);
                return;
            }
            //这边通过每次获取Actor位置的时候判断服务器是否下线来做的
            //所以暂时不需要处理
            //服务器要下线, 要把所有在这个服务器上的玩家清掉
            //但是自己的不能清掉
        }

        public async Task<RpcMessage> TrySendRpcRequest(PlacementFindActorPositionRequest actor,
                                                        object message,
                                                        bool needClearPosition) 
        {
            if (needClearPosition) this.placement.ClearActorPositionCache(actor);
            for (int i = 0; i < 2; ++i) 
            {
                try
                {
                    var position = await this.placement.FindActorPositonAsync(actor).ConfigureAwait(false);
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
                            return await this.SendRpcMessage(server, message).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(PlacementException)) 
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                    this.logger.LogError("TrySendRpcRequest, Actor:{0}@{1}, Exception:{2}",
                        actor.ActorType, actor.ActorID, e);
                }
            }

            throw new Exception("TrySendRpcRequest more then 2 times");
        }

        public IChannel GetChannelByServerID(long serverID) 
        {
            return this.clientConnectionPool.GetChannelByServerID(serverID);
        }

        private static readonly RpcMessage EmptyRpcResponse = new RpcMessage() 
        {
            Meta = new RpcResponse(),
        };

        public Task<RpcMessage> SendRpcMessage(IChannel channel, object message)
        {
            var rpcMessage = message as RpcMessage;
            Contract.Assert(rpcMessage != null);
            Contract.Assert(channel != null);
            var request = rpcMessage.Meta as RpcRequest;
            Contract.Assert(request != null);

            request.RequestId = this.NewSequenceID;

            var outboundMessage = new OutboundMessage(channel, message);
            this.messageCenter.SendMessage(outboundMessage);

            if (this.logger.IsEnabled(LogLevel.Trace)) 
            {
                this.logger.LogTrace("SendRpcMessage, Actor:{0}, Service:{1}, Method:{2}, ReentrantId:{3}, RequestId:{4}, CallId:{5}",
                    request.ActorId, request.ServiceName, request.MethodName, request.ReentrantId, request.RequestId, request.CallId);
            }

            if (request.Oneway) 
            {
                return Task.FromResult(EmptyRpcResponse);
            }

            var completionSource = new GenericCompletionSource<RpcMessage>();
            completionSource.ID = request.RequestId;
            this.taskCompletionSourceManager.Push(completionSource);
            return completionSource.GetTask() as Task<RpcMessage>;
        }

        private void ProcessRpcResponseError(RpcResponse msg, IGenericCompletionSource completionSource) 
        {
            if (msg.ErrorCode == (int)RpcErrorCode.ActorHasNewPosition)
            {
                completionSource.WithException(new RpcNewPositionException());
            }
            else if (msg.ErrorCode == (int)RpcErrorCode.MethodNotFound)
            {
                completionSource.WithException(new RpcDispatchException("Method Not Found"));
            }
            else 
            {
                completionSource.WithException(new Exception(msg.ErrorText));
            }
        }

        private void ProcessRpcHeartBeatResponse(InboundMessage message) 
        {
            var msg = (message.Inner as RpcMessage).Meta as RpcHeartBeatResponse;
            if (msg == null) 
            {
                this.logger.LogError("ProcessRpcHeartBeat input message type:{0}", message.GetType());
                return;
            }
            var elapsedTime = Platform.GetMilliSeconds() - msg.ResponseMilliseconds;
            if (elapsedTime >= 5) 
            {
                var sessionInfo = message.SourceConnection.GetSessionInfo();
                this.logger.LogWarning("ProcessRpcHearBeat, SessionID:{0}, ServerID:{1}, RemoteAddress:{2}, Elapsed Time:{3}ms",
                    sessionInfo.SessionID, sessionInfo.ServerID, sessionInfo.RemoteAddress, elapsedTime);
            }
        }

        private void ProcessRpcResponse(InboundMessage message)
        {
            var msg = message.Inner as RpcMessage;
            if (msg == null)
            {
                this.logger.LogError("ProcessRpcResponse input message type:{0}", message.Inner.GetType());
                return;
            }
            var response = msg.Meta as RpcResponse;
            var body = msg.Body;

            if (this.logger.IsEnabled(LogLevel.Trace)) 
            {
                this.logger.LogTrace("RpcResponse, RequestId:{0}, CallId:{1}, ErrorCode:{2}, ErrorText:{3}", 
                                    response.RequestId, response.CallId,
                                    response.ErrorCode, response.ErrorText);
            }

            var completionSource = this.taskCompletionSourceManager.GetCompletionSource(response.RequestId);
            if (completionSource == null) 
            {
                this.logger.LogWarning("ProcessRpcResponse fail, ResponseID:{0}", response.RequestId);
                return;
            }

            if (response.ErrorCode != 0) 
            {
                this.ProcessRpcResponseError(response, completionSource);
                return;
            }

            completionSource.WithResult(msg);
        }
    }
}
