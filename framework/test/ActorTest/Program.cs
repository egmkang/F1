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

        Task<string> GetTwoNamesAsync(string id);
    }

    [Rpc]
    public interface ITestActorBInterface 
    {
        Task<string> GetTwoNamesAsync(string id);
    }

    public class TestActor2Impl : Actor, ITestActorBInterface
    {
        public async Task<string> GetTwoNamesAsync(string id)
        {
            var proxy = this.GetActorProxy<ITestActorInterface>(id);
            var nameA = await proxy.HelloAsync();
            return $"MYNAME:{this.ID}, and caller name:{nameA}";
        }
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

        public async Task<string> GetTwoNamesAsync(string id)
        {
            var proxy = this.GetActorProxy<ITestActorBInterface>(id);
            var twoNames = await proxy.GetTwoNamesAsync(this.ID);
            return twoNames;
        }
    }

    class Program
    {

        static async Task RunReentrantTest(IServiceProvider serviceProvider, string id, string idB) 
        {
            await Task.Delay(20 * 1000);

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("test");
            var clientFactory = serviceProvider.GetRequiredService<IActorClientFactory>();
            var proxyA = clientFactory.GetActorProxy<ITestActorInterface>(id);

            var name = await proxyA.GetTwoNamesAsync(idB);
            logger.LogInformation("TwoNames:{0}", name);
        }

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
            builder.ServiceCollection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddNLog();
            });
            builder.Build();

            await builder.InitAsync("127.0.0.1:2379", 10001);

            _ = RunTest(builder.ServiceProvider, "A");
            _ = RunTest(builder.ServiceProvider, "B");
            _ = RunReentrantTest(builder.ServiceProvider, "CCCC", "DDDD");

            while (true)
            {
                await Task.Delay(10000);
            }
        }
    }
}
