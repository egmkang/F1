using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Google.Protobuf;
using RpcMessage;
using F1.Abstractions.RPC;
using F1.Abstractions.Actor;
using F1.Abstractions.Network;
using F1.Core.RPC;
using F1.Core.Utils;

namespace F1.Core.Actor
{
    internal class ActorContext : IActorContext
    {
        private readonly AsyncQueue<InboundMessage> mailBox = new AsyncQueue<InboundMessage>();
        private readonly ILogger logger;
        internal Actor Actor { get; set; }
        internal IParametersSerializer Serializer { get; set; }
        internal DispatchHandler Dispatcher { get; set; }
        internal IMessageCenter MessageCenter { get; set; }
        internal volatile bool stop = false;

        public (long ServerID, long RequestID) CurrentRequest { get; internal set; }

        public long RunningLoopID { get; internal set; }

        public long LastMessageTime { get; internal set; }

        public bool Loaded { get; internal set; }

        public ActorContext(Actor actor, ILogger logger) 
        {
            this.Actor = actor;
            this.logger = logger;

            this.LastMessageTime = Platform.GetMilliSeconds();
            this.Loaded = false;

            InitByteStringData();
        }

        public void SendMail(InboundMessage inboundMessage)
        {
            if (inboundMessage.Inner is RequestRpc requestRpc)
            {
                //logger.LogDebug("Actor:{0}, InputMessage:{1}_{2}",
                //                this.Actor.UniqueID, requestRpc.SrcServer, requestRpc.SrcRequestId);
                //可重入
                if (requestRpc.SrcServer == this.CurrentRequest.ServerID &&
                    requestRpc.SrcRequestId == this.CurrentRequest.RequestID)
                {
                    Task.Run(async () => await this.DispatchMessage(inboundMessage));
                    return;
                }
            }
            this.mailBox.Enqueue(inboundMessage);
        }

        public void Stop()
        {
            this.stop = true;
            this.mailBox.Enqueue(new InboundMessage());
        }

        public void Run()
        {
            _ = this.RunningLoop();
        }

        delegate byte[] GetByteStringDataFn(ByteString bytes);
        private static GetByteStringDataFn getByteStringData = null;

        private static void InitByteStringData() 
        {
            if (getByteStringData == null) 
            {
                //减少一次拷贝
                FieldInfo field = typeof(ByteString).GetField("bytes", BindingFlags.NonPublic | BindingFlags.Instance);
                getByteStringData = (data) => 
                {
                    var d = field.GetValue(data);
                    return d as Byte[];
                };
            }
        }
        private async Task DispatchMessage(InboundMessage inboundMessage) 
        {
            //TODO
            //timer目前还未处理
            //actor只处理rpc请求和timer请求
            if (inboundMessage.Inner is RequestRpc requestRpc)
            {
                this.CurrentRequest = (requestRpc.SrcServer, requestRpc.SrcRequestId);

                var inputTypes = this.Dispatcher.GetInputArgsType(requestRpc.Method);
                var inputArgs = this.Serializer.Deserialize(getByteStringData(requestRpc.Args), inputTypes);
                try
                {
                    var asyncReturnValue = this.Dispatcher.Invoke(requestRpc.Method, this.Actor, inputArgs);
                    var value = await asyncReturnValue.GetReturnValueAsync();
                    ActorUtils.SendResponseRpc(inboundMessage, this.MessageCenter, value, this.Serializer);
                    this.LastMessageTime = Platform.GetMilliSeconds();
                }
                catch (Exception e)
                {
                    this.logger.LogError("DispatchMessage Fail, ID:{0}, Exception:{1}",
                        this.Actor.UniqueID, e.ToString());

                    if (e is RpcDispatchException)
                    {
                        ActorUtils.SendRepsonseRpcError(inboundMessage, this.MessageCenter, RpcErrorCode.MethodNotFound, e.ToString());
                    }
                    else 
                    {
                        ActorUtils.SendRepsonseRpcError(inboundMessage, this.MessageCenter, RpcErrorCode.Others, e.ToString());
                    }
                }
                finally 
                {
                    this.CurrentRequest = (0, 0);
                }
            }
            else 
            {
                await this.Actor.DispatchUserMessage(inboundMessage);
            }
        }

        private async Task RunningLoop() 
        {
            await this.Actor.ActivateAsync();

            while (!this.stop)
            {
                await Task.Yield();

                var queue = await this.mailBox.ReadAsync();

                while (queue.TryDequeue(out var inboundMessage) && inboundMessage.Inner != null)
                {
                    try 
                    {
                        await this.DispatchMessage(inboundMessage);
                    }
                    catch (Exception e) 
                    {
                        this.logger.LogError("ActorMessageLoop, ID:{0}, Exception:{1}",
                            this.Actor.UniqueID, e.ToString());
                    }
                }
            }

            await this.Actor.DeactivateAsync();
        }
    }
}
