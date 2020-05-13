using System;
using System.Threading.Tasks;
using System.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using F1.Core.Core;
using F1.Core.RPC;
using F1.Core.Actor;
using F1.Abstractions.Actor;

namespace ActorTest
{
    [Rpc]
    public interface ITestActorInterface 
    {
        Task<int> RandomIntAsync();

        Task<string> HelloAsync();
    }

    public class TestActorImpl : Actor, ITestActorInterface
    {
        public Task<string> HelloAsync()
        {
            return Task.FromResult($"MyName is {this.ID}");
        }

        private readonly Random r = new Random();
        public Task<int> RandomIntAsync()
        {
            return Task.FromResult(r.Next());
        }
    }

    class Program
    {
        static async Task RunTest(IServiceProvider serviceProvider, string id) 
        {
            await Task.Delay(16 * 1000);
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("test");
            var clientFactory = serviceProvider.GetRequiredService<IActorClientFactory>();
            var proxyA = clientFactory.GetActorProxy<ITestActorInterface>(id);

            logger.LogInformation("BeginHelloAsync, ID:{0}", id);
            var hello = await proxyA.HelloAsync();
            logger.LogInformation("HelloAsync, ID:{1}, Result:{0}", hello, id);

            logger.LogInformation("BeginRandomIntAsync, ID:{0}", id);
            var randomNumber = await proxyA.RandomIntAsync();
            logger.LogInformation("RandomIntAsync, ID:{1}, Result:{0}", randomNumber, id);
        }

        static async Task Main(string[] args)
        {
            ProfileOptimization.SetProfileRoot("./profile");
            ProfileOptimization.StartProfile("actor_test");

            var builder = new ServiceBuilder();
            builder.AddDefaultServices();
            builder.ServiceCollection.AddLogging( builder => 
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddNLog();
            });
            builder.Build();

            await builder.InitAsync("127.0.0.1:2379", 10001);

            _ = RunTest(builder.ServiceProvider, "A");
            _ = RunTest(builder.ServiceProvider, "B");

            while (true)
            {
                await Task.Delay(10000);
            }
        }
    }
}
