using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AspectCore.Extensions.Reflection;
using Google.Protobuf;
using RpcMessage;
using F1.Abstractions.Placement;
using F1.Abstractions.RPC;
using F1.Abstractions.Actor;

namespace F1.Core.RPC
{
    /// <summary>
    /// 用来做hook用(单元测试), 其他的地方不用
    /// </summary>
    public delegate Task<ResponseRpc> TrySendRpcRequestFunc(PlacementFindActorPositionRequest actor,
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
        public Type Type { get; internal set; }

        public PlacementFindActorPositionRequest PositionRequest { get; internal set; }

        public ILogger Logger { get; internal set; }
        public IServiceProvider ServiceProvider { get; internal set; }
        internal RpcClientFactory RpcClientFactory { get; set; }
        public RpcDispatchProxyFactory DispatchProxyFactory { get; internal set; }
        public IParametersSerializer Serializer { get; internal set; }
        public TrySendRpcRequestFunc SendHook { get; set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var handler = this.DispatchProxyFactory.GetProxyClientHandler(this.Type, targetMethod);
            if (handler == null) 
            {
                this.Logger.LogError("Actor:{0}, Type:{1}, Method:{2} not found", this.ActorUniqueID, this.Type.Name, targetMethod.Name);
                throw new RpcDispatchException("Method not found");
            }

            var taskCompletionSource = handler.NewCompletionSource();

            //实际上Context是不能为空的
            var currentRequest = this.Context == null ? (0, 0) : this.Context.CurrentRequest;
            var request = new RequestRpc()
            {
                ActorType = this.PositionRequest.ActorType,
                ActorId = this.PositionRequest.ActorID,
                Method = handler.Name,

                Args = ByteString.CopyFrom(this.Serializer.Serialize(args, handler.ParametersType)),
                NeedResult = !handler.IsOneWay,
                RequestId = taskCompletionSource.ID,

                SrcServer = currentRequest.ServerID,
                SrcRequestId = currentRequest.RequestID,
            };

            _ = this.TrySendRpcRequest(request, taskCompletionSource, handler);
            return taskCompletionSource.GetTask();
        }

        private static object Empty = new object();
        private async Task TrySendRpcRequest(RequestRpc request,
                                            IGenericCompletionSource completionSource,
                                            RequestDisptachProxyClientHandler handler) 
        {
            try 
            {
                ResponseRpc response;

                if (this.SendHook != null)
                {
                    response = await this.SendHook(this.PositionRequest, request, false);
                }
                else 
                {
                    response = await this.RpcClientFactory.TrySendRpcRequest(this.PositionRequest, request, false);
                }

                if (response != null) 
                {
                    if (response.Response == null || response.Response.Length == 0) 
                    {
                        completionSource.WithResult(Empty);
                    }
                    var o = this.Serializer.Deserialize(response.Response.ToByteArray(), handler.ReturnType);
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
