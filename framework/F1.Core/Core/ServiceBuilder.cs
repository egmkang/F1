using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using F1.Abstractions;
using F1.Abstractions.Network;
using System.Threading.Tasks;
using F1.Core.Actor;

namespace F1.Core.Core
{
    public class ServiceBuilder : IServiceBuilder
    {
        private readonly ServiceCollection serviceCollection = new ServiceCollection();
        private IServiceProvider serviceProvider;
        public IServiceProvider ServiceProvider => this.serviceProvider;
        public IServiceCollection ServiceCollection => this.serviceCollection;

        public IServiceBuilder Build()
        {
            this.serviceProvider = serviceCollection.BuildServiceProvider();
            return this;
        }

        public async Task InitAsync() 
        {
            var runtime = this.serviceProvider.GetRequiredService<ActorRuntime>();
            await runtime.InitActorRuntime();
        }

        public void ShutDown()
        {
            var listener = this.serviceProvider.GetRequiredService<IConnectionListener>();
            listener.ShutdDownAsync().Wait();
            var connectionFactory = this.serviceProvider.GetRequiredService<IClientConnectionFactory>();
            connectionFactory.ShutdDownAsync().Wait();
        }
    }
}
