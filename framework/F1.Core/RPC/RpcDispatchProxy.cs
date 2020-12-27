using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AspectCore.Extensions.Reflection;
using Google.Protobuf;
using F1.Abstractions.Placement;
using F1.Abstractions.RPC;
using F1.Abstractions.Actor;
using F1.Core.Message;
using Rpc;

namespace F1.Core.RPC
{
    /// <summary>
    /// 用来做hook用(单元测试), 其他的地方不用
    /// </summary>
    public delegate Task<RpcMessage> TrySendRpcRequestFunc(PlacementFindActorPositionRequest actor,
                                                 object message,
                                                 bool needClearPosition);

    public class RpcDispatchProxy : DispatchProxy
    {
        /// <summary>
        /// Actor的上下文
        /// </summary>
        public IActorContext Context { get; set; }

        /// <summary>
        /// Actor的ID
        /// </summary>
        public string ActorUniqueID { get; internal set; }

        /// <summary>
        /// Proxy的interface类型
        /// </summary>
        public Type InterfaceType { get; internal set; }

        /// <summary>
        /// 实现interface的类型
        /// </summary>
        public Type ImplType { get; internal set; }

        public PlacementFindActorPositionRequest PositionRequest { get; internal set; }

        public ILogger Logger { get; internal set; }
        public IServiceProvider ServiceProvider { get; internal set; }
        internal RpcClientFactory RpcClientFactory { get; set; }
        public RpcDispatchProxyFactory DispatchProxyFactory { get; internal set; }
        public IParametersSerializer Serializer { get; internal set; }
        public TrySendRpcRequestFunc SendHook { get; set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var handler = this.DispatchProxyFactory.GetProxyClientHandler(this.InterfaceType, targetMethod);
            if (handler == null) 
            {
                this.Logger.LogError("Actor:{0}, Type:{1}, Method:{2} not found", this.ActorUniqueID, this.InterfaceType.Name, targetMethod.Name);
                throw new RpcDispatchException("Method not found");
            }

            var taskCompletionSource = handler.NewCompletionSource();

            var rpcMessage = new RpcMessage()
            {
                Meta = new RpcRequest() 
                {
                    ServiceName = this.PositionRequest.ActorType,
                    MethodName = handler.MethodName,
                    ActorId = this.PositionRequest.ActorID,
                    Oneway = handler.IsOneWay,
                    CallId = taskCompletionSource.ID,

                    ReentrantId = this.Context.ReentrantId,
                    //TODO: tracing
                },
                Body = this.Serializer.Serialize(args, handler.ParametersType),
            };

            _ = this.TrySendRpcRequest(rpcMessage, taskCompletionSource, handler);
            return taskCompletionSource.GetTask();
        }

        private static object Empty = new object();
        private async Task TrySendRpcRequest(RpcMessage request,
                                            IGenericCompletionSource completionSource,
                                            RequestDisptachProxyClientHandler handler) 
        {
            try 
            {
                RpcMessage response;

                if (this.SendHook != null)
                {
                    response = await this.SendHook(this.PositionRequest, request, false).ConfigureAwait(false);
                }
                else 
                {
                    response = await this.RpcClientFactory.TrySendRpcRequest(this.PositionRequest, request, false).ConfigureAwait(false);
                }

                if (response != null) 
                {
                    if (response.Body.Length == 0) 
                    {
                        completionSource.WithResult(Empty);
                    }
                    //TODO
                    var o = this.Serializer.Deserialize(response.Body, handler.ReturnType);
                    completionSource.WithResult(o);
                }
            }
            catch (Exception e) 
            {
                completionSource.WithException(e);
            }
        }
    }
}
