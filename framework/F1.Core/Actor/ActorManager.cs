using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;
using Google.Protobuf;
using RpcMessage;
using F1.Core.Utils;
using F1.Core.RPC;
using F1.Abstractions.Network;
using System.Collections.Generic;
using F1.Abstractions.Placement;
using MessagePack.Formatters;

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

        public ActorManager(ILoggerFactory loggerFactory,
                            ActorFactory actorFactory,
                            RpcMetadata rpcMetadata,
                            IMessageCenter messageCenter,
                            IPlacement placement
                            ) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
            this.actorFactory = actorFactory;
            this.rpcMetadata = rpcMetadata;
            this.messageCenter = messageCenter;
            this.placement = placement;

            this.messageCenter.RegisterMessageProc(typeof(RequestRpc).FullName, this.ProcessRequestRpc);
            this.messageCenter.RegisterMessageProc(typeof(RequestRpcHeartBeat).FullName, this.ProcessRequestRpcHeartBeat);

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
                var response = await this.placement.FindActorPositonAsync(args);
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
