using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;
using Google.Protobuf;
using F1.Core.RPC;
using F1.Abstractions.Network;
using RpcMessage;

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
                var actor = this.GetActor(requestRpc.ActorType, requestRpc.ActorId);
                if (actor == null)
                {
                    this.logger.LogError("ProcessRequestRpc Actor not found, Type:{0}, ID:{1}", requestRpc.ActorType, requestRpc.ActorId);
                    ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, 100, "GetActor fail");
                    return;
                }
                actor.Context.SendMail(inboundMessage);
            }
            catch (Exception e) 
            {
                this.logger.LogError("ProcessRequestRpc, Type:{0}, ID:{1}, Exception:{2}",
                    requestRpc.ActorType, requestRpc.ActorId, e.ToString());

                ActorUtils.SendRepsonseRpcError(inboundMessage, this.messageCenter, 100, e.ToString());
            }
        }
    }
}
