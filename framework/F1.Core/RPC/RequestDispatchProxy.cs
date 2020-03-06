using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AspectCore.Extensions.Reflection;
using F1.Abstractions.Placement;

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


            this.TrySendRpcRequest(taskCompletionSource);

            return taskCompletionSource.GetTask();
        }

        private async void TrySendRpcRequest(IGenericCompletionSource completionSource) 
        {
            try 
            { 
                //TODO:
                //1. find position
                //2. make request 
                //3. make TaskCompletionSource and await Response
                //4. analysis response, and consider retry
            }
            catch (Exception e) 
            {
            }
            await Task.CompletedTask;
        }
    }
}
