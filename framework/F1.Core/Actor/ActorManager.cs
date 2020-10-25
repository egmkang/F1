using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using RpcMessage;
using F1.Core.Utils;
using F1.Core.RPC;
using F1.Core.Message;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Network;

namespace F1.Core.Actor
{
    internal class ActorManager
    {
        const int ActorLifeTime = 30 * 60 * 1000;
        const int ActorGCInterval = 1 * 60 * 1000;

        private readonly object mutex = new object();
        private readonly ConcurrentDictionary<(Type, string), Actor> actorInstances = new ConcurrentDictionary<(Type, string), Actor>();
        private readonly ILogger logger;
        private readonly ActorFactory actorFactory;
        private readonly RpcMetadata rpcMetadata;
        private readonly IMessageCenter messageCenter;
        private readonly IPlacement placement;
        private readonly IServiceProvider serviceProvider;
        private ClientConnectionPool clientConnectionPool;

        public ActorManager(ILoggerFactory loggerFactory,
                            ActorFactory actorFactory,
                            RpcMetadata rpcMetadata,
                            IMessageCenter messageCenter,
                            IPlacement placement,
                            IServiceProvider serviceProvider
                            )
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
            this.actorFactory = actorFactory;
            this.rpcMetadata = rpcMetadata;
            this.messageCenter = messageCenter;
            this.placement = placement;
            this.serviceProvider = serviceProvider;

            this.messageCenter.RegisterTypedMessageProc<RequestRpc>(this.ProcessRequestRpc);
            this.messageCenter.RegisterTypedMessageProc<RequestRpcHeartBeat>(this.ProcessRequestRpcHeartBeat);
            this.messageCenter.RegisterUserMessageCallback((type, actorID, inboundMessage) => this.DispatchUserMessage(inboundMessage, type, actorID));

            _ = Util.RunTaskTimer(this.ActorGC, ActorGCInterval);
        }

        public Actor GetActor(string type, string uniqueID)
        {
            this.rpcMetadata.RpcServerTypes.TryGetValue(type, out var serverType);
            if (serverType == null)
            {
                throw new Exception($"DestType:{type} not found");
            }

            actorInstances.TryGetValue((serverType, uniqueID), out var actor);
            if (actor != null)
            {
                return actor;
            }

            lock (mutex)
            {
                actorInstances.TryGetValue((serverType, uniqueID), out actor);
                if (actor != null)
                {
                    return actor;
                }

                actor = this.actorFactory.CreateActor(serverType, uniqueID);
                Contract.Assert(actor != null);

                this.actorInstances.TryAdd((serverType, uniqueID), actor);
                return actor;
            }
        }

        private void ActorGC()
        {
            //暂定1分钟做一次GC
            //关掉半个小时内还未活跃的Actor
            var list = new List<(Type, string, Actor)>();
            var lastMessageTime = Platform.GetMilliSeconds() - ActorLifeTime;

            foreach (var ((type, actorID), actor) in this.actorInstances)
            {
                if (actor.Context.LastMessageTime < lastMessageTime)
                {
                    list.Add((type, actorID, actor));
                }
            }
            foreach (var (type, actorID, actor) in list)
            {
                if (actor.Context.LastMessageTime < lastMessageTime)
                {
                    this.actorInstances.TryRemove((type, actorID), out var _);
                    actor.Context.Stop();
                    this.logger.LogInformation("ActorGC, Actor:{0}", actor.UniqueID);
                }
            }
        }

        private async Task ProcessRequestRpcSlowPath(InboundMessage inboundMessage)
        {
            var requestRpc = inboundMessage.Inner as RequestRpc;
            Contract.Assert(requestRpc != null);

            var implType = this.rpcMetadata.GetServerType(requestRpc.ActorType);
            if (implType == null) 
            {
                this.logger.LogError("DispatchRpcRequestSlowPath, InterfaceType:{0} not found ImplType", requestRpc.ActorType);
                return;
            }
            var args = new PlacementFindActorPositionRequest()
            {
                ActorType = requestRpc.ActorType,
                ActorID = requestRpc.ActorId,
                TTL = 0,
            };

            try
            {
                //慢路径, 需要到pd里面查询是否没问题
                //这边要处理Actor的位置, 万一Actor的位置发生变化
                //那么需要告诉对端重新请求
                var response = await this.placement.FindActorPositonAsync(args).ConfigureAwait(false);
                if (response != null && response.ServerID == this.placement.CurrentServerID)
                {
                    this.DisptachRequestRPC(inboundMessage, requestRpc);
                }
                else
                {
                    if (response == null)
                    {
                        ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, RpcErrorCode.ActorPositionNotFound, "PositionNotFound");
                    }
                    else
                    {
                        ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, RpcErrorCode.ActorHasNewPosition, "Actor has new position");
                    }
                }
            }
            catch (Exception e)
            {
                ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, RpcErrorCode.Others, e.ToString());
            }
        }

        private void ProcessRequestRpcHeartBeat(InboundMessage inboundMessage)
        {
            var response = new ResponseRpcHeartBeat()
            {
                MilliSeconds = (inboundMessage.Inner as RequestRpcHeartBeat).MilliSeconds,
            };
            this.messageCenter.SendMessage(new OutboundMessage(inboundMessage.SourceConnection, response));
            //this.logger.LogInformation("ProcessRequestRpcHeartBeat, SessionID:{0}", inboundMessage.SourceConnection.GetSessionInfo().SessionID);
        }

        private void DisptachRequestRPC(InboundMessage inboundMessage, RequestRpc requestRpc)
        {
            var actor = this.GetActor(requestRpc.ActorType, requestRpc.ActorId);
            if (actor == null)
            {
                this.logger.LogError("ProcessRequestRpc Actor not found, ID:{0}@{1}", requestRpc.ActorType, requestRpc.ActorId);
                ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, RpcErrorCode.Others, "GetActor fail");
                return;
            }
            actor.Context.SendMail(inboundMessage);
        }

        /// <summary>
        /// 把用户消息发送到Actor的队列里面去
        /// </summary>
        /// <param name="inboundMessage">用户消息</param>
        /// <param name="type">Actor的类型</param>
        /// <param name="actorID">Actor的ID</param>
        /// <returns>成功返回true</returns>
        public bool DispatchUserMessage(InboundMessage inboundMessage, string type, string actorID) 
        {
            var implType = this.rpcMetadata.GetServerType(type);
            if (implType == null) 
            {
                this.logger.LogError("DispatchUserMessage, InterfaceType:{0} not found ImplType:{1}", type);
                return false;
            }

            var args = pdPositionArgsCache.Value;
            args.ActorType = type;
            args.ActorID = actorID;
            args.TTL = 0;

            try 
            {
                var destPosition = this.placement.FindActorPositionInCache(args);
                if (destPosition != null && destPosition.ServerID == this.placement.CurrentServerID)
                {
                    var actor = this.GetActor(type, actorID);
                    actor.Context.SendMail(inboundMessage);
                    return true;
                }
                else 
                {
                    _ = this.DispatchUserMessageSlowPath(inboundMessage, type, actorID);
                }
            }
            catch (Exception e) 
            {
                this.logger.LogError("DispatchUserMessage, Actor:{0}/{1}, Error:{2}", type, actorID, e.ToString());
            }
            return false;
        }

        private IChannel GetServerChannelByID(long serverID) 
        {
            if (this.clientConnectionPool == null) 
            {
                this.clientConnectionPool = this.serviceProvider.GetRequiredService<ClientConnectionPool>();
            }
            if (this.clientConnectionPool != null) 
            {
                return this.clientConnectionPool.GetChannelByServerID(serverID);
            }
            return null;
        }

        private async Task DispatchUserMessageSlowPath(InboundMessage inboundMessage, string type, string actorID) 
        {
            var implType = this.rpcMetadata.GetServerType(type);
            if (implType == null) 
            {
                this.logger.LogError("DispatchUserMessageSlowPath, InterfaceType:{0} not found ImplType", type);
                return;
            }
            var args = new PlacementFindActorPositionRequest()
            {
                ActorType = type,
                ActorID = actorID,
                TTL = 0,
            };

            try
            {
                //慢路径, 需要到pd里面查询是否没问题
                //这边要处理Actor的位置, 万一Actor的位置发生变化
                //这边帮Gateway把消息转发到对应的服务器上面去
                var response = await this.placement.FindActorPositonAsync(args).ConfigureAwait(false);
                if (response != null && response.ServerID == this.placement.CurrentServerID)
                {
                    var actor = this.GetActor(type, actorID);
                    actor.Context.SendMail(inboundMessage);
                    return;
                }
                else  if (response != null)
                {
                    var channel = this.GetServerChannelByID(response.ServerID);
                    this.messageCenter.SendMessage(new OutboundMessage(channel, inboundMessage.Inner));
                    return;
                }
                logger.LogError("DispatchUserMessageSlowPath, ActorType:{0} ActorID:{1} not found", type, actorID);
            }
            catch (Exception e) 
            {
                logger.LogError("DispatchUserMessageSlowPath, ActorType:{0} ActorID:{1}, Exception:{2}", type, actorID, e);
            }
        }

        static ThreadLocal<PlacementFindActorPositionRequest> pdPositionArgsCache = new ThreadLocal<PlacementFindActorPositionRequest>(() => new PlacementFindActorPositionRequest());

        private void ProcessRequestRpc(InboundMessage inboundMessage) 
        {
            var requestRpc = inboundMessage.Inner as RequestRpc;
            if (requestRpc == null) 
            {
                this.logger.LogError("ProcessRequestRpc input message type is {0}", inboundMessage.Inner.GetType());
                return;
            }
            try
            {
                var implType = this.rpcMetadata.GetServerType(requestRpc.ActorType);
                if (implType == null) 
                {
                    this.logger.LogError("ProcessRequestRpc, InterfaceType:{0} not found ImplType", requestRpc.ActorType);
                    return;
                }
                var args = pdPositionArgsCache.Value;
                args.ActorType = requestRpc.ActorType;
                args.ActorID = requestRpc.ActorId;
                args.TTL = 0;

                //这边通过本地的缓存检测服务器是否存在, 存在就直接执行
                //否则需要进行慢路径确认
                var destPosition = this.placement.FindActorPositionInCache(args);
                if (destPosition != null && destPosition.ServerID == this.placement.CurrentServerID)
                {
                    this.DisptachRequestRPC(inboundMessage, requestRpc);
                }
                else 
                {
                    _ = this.ProcessRequestRpcSlowPath(inboundMessage);
                }
            }
            catch (Exception e) 
            {
                this.logger.LogError("ProcessRequestRpc, ID:{0}@{1}, Exception:{2}",
                    requestRpc.ActorType, requestRpc.ActorId, e.ToString());

                ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, RpcErrorCode.Others, e.ToString());
            }
        }
    }
}
