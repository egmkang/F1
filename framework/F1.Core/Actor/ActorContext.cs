using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Google.Protobuf;
using DotNetty.Transport.Channels;
using F1.Abstractions.RPC;
using F1.Abstractions.Actor;
using F1.Abstractions.Network;
using F1.Core.RPC;
using F1.Core.Utils;
using F1.Core.Message;
using Rpc;

namespace F1.Core.Actor
{
    internal class ActorContext : IActorContext
    {
        private readonly AsyncQueue<InboundMessage> mailBox = new AsyncQueue<InboundMessage>();
        private readonly ILogger logger;
        internal Actor Actor { get; set; }
        internal IParametersSerializerFactory SerializerFactory { get; set; }
        internal DispatchHandler Dispatcher { get; set; }
        internal volatile bool stop = false;

        /// <summary>
        /// 可重入ID
        /// </summary>
        public string ReentrantId { get; internal set; }

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
            if (inboundMessage.Inner is RpcMessage request)
            {
                //logger.LogDebug("Actor:{0}, InputMessage:{1}",
                //                this.Actor.UniqueID, requestMeta.ReentrantId);

                //可重入
                if ((request.Meta as RpcRequest).ReentrantId == this.ReentrantId)
                {
                    _ = this.DispatchMessage(inboundMessage).ConfigureAwait(false);
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
            //actor只处理rpc请求和timer请求
            if (inboundMessage.Inner is ActorTimer timer)
            {
                timer.Tick();
            }
            else if (inboundMessage.Inner is RpcMessage rpcMessage)
            {
                await this.DispatchRpcRequest(inboundMessage.SourceConnection, rpcMessage).ConfigureAwait(false);
            }
            else 
            {
                await this.Actor.DispatchUserMessage(inboundMessage).ConfigureAwait(false);
            }
        }

        private async Task DispatchRpcRequest(IChannel sourceChannel, RpcMessage rpcMessage)
        {
            var requestMeta = rpcMessage.Meta as RpcRequest;

            this.ReentrantId = requestMeta.ReentrantId;

            if (this.logger.IsEnabled(LogLevel.Trace)) 
            {
                this.logger.LogTrace("Actor:{0} ReentranId:{1}, Call:{2}.{3}, RequestId:{4}, CallId:{5}",
                                        this.Actor.UniqueID, this.ReentrantId,
                                        requestMeta.ServiceName, requestMeta.MethodName,
                                        requestMeta.RequestId, requestMeta.CallId);
            }

            var inputTypes = this.Dispatcher.GetInputArgsType(requestMeta.ServiceName, requestMeta.MethodName);
            var serializer = this.SerializerFactory.GetSerializer(requestMeta.EncodingType);
            var inputArgs = serializer.Deserialize(rpcMessage.Body, inputTypes);
            try
            {
                var asyncReturnValue = this.Dispatcher.Invoke(requestMeta.ServiceName, requestMeta.MethodName, this.Actor, inputArgs);
                var value = await asyncReturnValue.GetReturnValueAsync().ConfigureAwait(false);
                ActorUtils.SendResponseRpc(sourceChannel, requestMeta, this.Actor.MessageCenter, value, serializer);
                if (this.logger.IsEnabled(LogLevel.Trace)) 
                {
                    this.logger.LogTrace("Actor:{0} ReentranId:{1}, Call:{2}.{3}, RequestId:{4}, CallId:{5} Response Returned",
                                            this.Actor.UniqueID, this.ReentrantId,
                                            requestMeta.ServiceName, requestMeta.MethodName,
                                            requestMeta.RequestId, requestMeta.CallId);
                }
                this.LastMessageTime = Platform.GetMilliSeconds();
            }
            catch (Exception e)
            {
                this.logger.LogError("DispatchMessage Fail, ID:{0}, Exception:{1}",
                    this.Actor.UniqueID, e.ToString());

                if (e is RpcDispatchException)
                {
                    ActorUtils.SendRepsonseRpcError(sourceChannel, requestMeta, this.Actor.MessageCenter, RpcErrorCode.MethodNotFound, e.ToString());
                }
                else
                {
                    ActorUtils.SendRepsonseRpcError(sourceChannel, requestMeta, this.Actor.MessageCenter, RpcErrorCode.Others, e.ToString());
                }
            }
            finally
            {
                this.ReentrantId = "";
            }
        }

        private async Task RunningLoop() 
        {
            await this.Actor.ActivateAsync().ConfigureAwait(false);

            //TODO: 判断协程ID

            while (!this.stop)
            {
                await Task.Yield();

                var queue = await this.mailBox.ReadAsync().ConfigureAwait(false);

                while (queue.TryDequeue(out var inboundMessage) && inboundMessage.Inner != null)
                {
                    try 
                    {
                        await this.DispatchMessage(inboundMessage).ConfigureAwait(false);
                    }
                    catch (Exception e) 
                    {
                        this.logger.LogError("ActorMessageLoop, ID:{0}, Exception:{1}",
                            this.Actor.UniqueID, e.ToString());
                    }
                }
            }

            await this.Actor.DeactivateAsync().ConfigureAwait(false);
        }
    }
}
