using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AspectCore.Extensions.Reflection;
using F1.Abstractions.Placement;
using RpcMessage;

namespace F1.Core.RPC
{
    public class RequestDispatchProxy : DispatchProxy
    {
        /// <summary>
        /// Actor的上下文
        /// </summary>
        public object Context { get; set; }

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
        public RequestDispatchProxyFactory DispatchProxyFactory { get; internal set; }


        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var handler = this.DispatchProxyFactory.GetProxyClientHandler(this.Type, targetMethod);
            if (handler == null) 
            {
                this.Logger.LogError("Actor:{0}, Type:{1}, Method:{2} not found", this.ActorUniqueID, this.Type.Name, targetMethod.Name);
                throw new RpcDispatchException("Method not found");
            }

            var taskCompletionSource = handler.NewCompletionSource();

            var request = new RequestRpc() 
            {
                ActorType = this.PositionRequest.ActorType,
                ActorId = this.PositionRequest.ActorID,
                Method = handler.Name,

                //TODO:
                //args

                NeedResult = !handler.IsOneWay,

                RequestId = taskCompletionSource.ID,

                //TODO:
                //context
            };

            _ = this.TrySendRpcRequest(request, taskCompletionSource);

            return taskCompletionSource.GetTask();
        }

        private static object Empty = new object();
        private async Task TrySendRpcRequest(RequestRpc request, IGenericCompletionSource completionSource) 
        {
            try 
            {
                var response = await this.RpcClientFactory.TrySendRpcRequest(this.PositionRequest, request, false);
                if (response != null) 
                {
                    if (response.Response == null || response.Response.Length == 0) 
                    {
                        completionSource.WithResult(Empty);
                    }
                    //TODO:
                    //decode response, and try set result
                }
            }
            catch (Exception e) 
            {
                completionSource.WithException(e);
            }
        }
    }
}
