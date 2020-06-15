﻿using System;
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

namespace F1.Core.Actor
{
    internal class ActorManager
    {
        private readonly object mutex = new object();
        private readonly ConcurrentDictionary<(Type, string), Actor> actorInstances = new ConcurrentDictionary<(Type, string), Actor>();
        private readonly ILogger logger;
        private readonly ActorFactory actorFactory;
        private readonly RpcMetadata rpcMetadata;
        private readonly IMessageCenter messageCenter;

        public ActorManager(ILoggerFactory loggerFactory,
                            ActorFactory actorFactory,
                            RpcMetadata rpcMetadata,
                            IMessageCenter messageCenter
                            ) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
            this.actorFactory = actorFactory;
            this.rpcMetadata = rpcMetadata;
            this.messageCenter = messageCenter;

            this.messageCenter.RegisterMessageProc(typeof(RequestRpc).FullName, this.ProcessRequestRpc);
            this.messageCenter.RegisterMessageProc(typeof(RequestRpcHeartBeat).FullName, this.ProcessRequestRpcHeartBeat);

            _ = Util.RunTaskTimer(this.ActorGC, 10 * 1000);
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


        const int ActorLifeTime = 30 * 60 * 1000;

        private void ActorGC() 
        {
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
            //TODO: ActorGC
            //暂定1分钟做一次GC
            //关掉半个小时内还未活跃的Actor
        }

        private async Task ProcessRequestRpcSlowPath(InboundMessage inboundMessage) 
        {
            //TODO: Actor慢路径
            //慢路径, 需要到pd里面查询是否没问题
            await Task.CompletedTask;
        }

        private void ProcessRequestRpcHeartBeat(InboundMessage inboundMessage) 
        {
            var response = new ResponseRpcHeartBeat() 
            {
                 MilliSeconds = (inboundMessage.Inner as RequestRpcHeartBeat).MilliSeconds,
            };
            this.messageCenter.SendMessage(new OutboundMessage(inboundMessage.SourceConnection, response));
        }

        private void ProcessRequestRpc(InboundMessage inboundMessage) 
        {
            //TODO: 处理Actor位置发生变化
            //这边要处理Actor的位置, 万一Actor的位置发生变化
            //那么需要告诉对端重新请求

            var requestRpc = inboundMessage.Inner as RequestRpc;
            if (requestRpc == null) 
            {
                this.logger.LogError("ProcessRequestRpc input message type is {0}", inboundMessage.Inner.GetType());
                return;
            }
            try
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
            catch (Exception e) 
            {
                this.logger.LogError("ProcessRequestRpc, ID:{0}@{1}, Exception:{2}",
                    requestRpc.ActorType, requestRpc.ActorId, e.ToString());

                ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, RpcErrorCode.Others, e.ToString());
            }
        }
    }
}
