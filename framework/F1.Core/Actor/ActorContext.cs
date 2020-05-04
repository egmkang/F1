using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Actor;
using F1.Abstractions.Network;
using F1.Core.Utils;
using System.Threading.Tasks;
using RpcMessage;
using F1.Core.RPC;
using F1.Abstractions.RPC;

namespace F1.Core.Actor
{
    internal class ActorContext : IActorContext
    {
        private readonly AsyncMessageQueue<InboundMessage> mailBox = new AsyncMessageQueue<InboundMessage>();
        private readonly ILogger logger;
        internal Actor Actor { get; set; }
        internal IParametersSerializer Serializer { get; set; }
        internal RequestDispatchHandler Dispatcher { get; set; }
        internal IMessageCenter MessageCenter { get; set; }

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
        }

        public void SendMail(InboundMessage inboundMessage)
        {
            if (inboundMessage.Inner is RequestRpc requestRpc)
            {
                //可重入
                if (requestRpc.SrcServer == this.CurrentRequest.ServerID &&
                    requestRpc.SrcRequestId == this.CurrentRequest.RequestID)
                {
                    Task.Run(async () => await this.DispatchMessage(inboundMessage));
                    return;
                }
            }
            this.mailBox.PushMessage(inboundMessage);
        }

        public void Stop()
        {
            this.mailBox.ShutDown(new InboundMessage());
        }

        public void Run()
        {
            _ = this.RunningLoop();
        }

        private async Task DispatchMessage(InboundMessage inboundMessage) 
        {
            //TODO
            //timer目前还未处理
            //actor只处理rpc请求和timer请求
            if (inboundMessage.Inner is RequestRpc requestRpc)
            {
                var inputTypes = this.Dispatcher.GetInputArgsType(requestRpc.Method);
                //TODO
                //这边需要优化, 多拷贝了一次
                var inputArgs = this.Serializer.Deserialize(requestRpc.Args.ToByteArray(), inputTypes);
                try
                {
                    var asyncReturnValue = this.Dispatcher.Invoke(requestRpc.Method, this.Actor, inputArgs);
                    var value = await asyncReturnValue.GetReturnValueAsync();
                    //TODO
                    //这边拿到了最终的结果, 可以发送返回
                    ActorUtils.SendResponseRpc(inboundMessage, this.MessageCenter, value, this.Serializer);
                }
                catch (Exception e) 
                {
                    this.logger.LogError("DispatchMessage Fail, Type:{0}, ID:{1}, Exception:{2}",
                        this.Actor.ActorType, this.Actor.ID, e.ToString());
                    //TODO
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
            var reader = this.mailBox.Reader;

            while (this.mailBox.Valid) 
            {
                var more = await reader.WaitToReadAsync();
                if (!more)
                {
                    break;
                }

                await Task.Yield();

                while (reader.TryRead(out var inboundMessage) && inboundMessage.Inner != null) 
                {
                    try 
                    {
                        await this.DispatchMessage(inboundMessage);
                    }
                    catch (Exception e) 
                    {
                        this.logger.LogError("ActorMessageLoop, Type:{0}, ID:{1}, Exception:{2}",
                            this.Actor.ActorType.Name, this.Actor.ID, e.ToString());
                    }
                }
            }

            await this.Actor.DeactivateAsync();
        }
    }
}
