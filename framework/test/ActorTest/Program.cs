using System;
using System.Threading.Tasks;
using System.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using DotNetty.Codecs;
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
        static async Task RunTest(IServiceProvider serviceProvider) 
        {
            await Task.Delay(16 * 1000);
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("test");
            var clientFactory = serviceProvider.GetRequiredService<IActorClientFactory>();
            var proxyA = clientFactory.GetActorProxy<ITestActorInterface>("A");

            logger.LogInformation("BeginHelloAsync");
            var hello = await proxyA.HelloAsync();
            logger.LogInformation("HelloAsync Result:{0}", hello);

            logger.LogInformation("BeginRandomIntAsync");
            var randomNumber = await proxyA.RandomIntAsync();
            logger.LogInformation("RandomIntAsync Result:{0}", randomNumber);

            await Task.CompletedTask;
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
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                builder.AddNLog();
            });
            builder.Build();

            await builder.InitAsync("127.0.0.1:2379", 10001);

            _ = RunTest(builder.ServiceProvider);

            while (true)
            {
                await Task.Delay(10000);
            }
        }
    }
}
