using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.Actor;
using F1.Core.RPC;
using Microsoft.Extensions.DependencyInjection;

namespace F1.Core.Actor
{
    public class ActorClientFactory : IActorClientFactory
    {
        private readonly ActorRuntime runtime;
        private readonly RpcDispatchProxyFactory dispatchProxyFactory;

        public ActorClientFactory(IServiceProvider serviceProvider) 
        {
            this.runtime = serviceProvider.GetRequiredService<ActorRuntime>();
            this.dispatchProxyFactory = serviceProvider.GetRequiredService<RpcDispatchProxyFactory>();
        }

        public T GetActorProxy<T>(string name)
        {
            return this.dispatchProxyFactory.CreateProxy<T>(name, runtime.ClientSideContext);
        }
    }
}
