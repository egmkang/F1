using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using F1.Core.Core;

namespace ActorTest
{
    class Program
    {
        static async Task RunTest(IServiceProvider serviceProvider) 
        {
            await Task.CompletedTask;
        }

        static async Task Main(string[] args)
        {
            var builder = new ServiceBuilder();
            builder.AddDefaultServices();
            builder.ServiceCollection.AddLogging(opt => opt.AddConsole());
            builder.Build();

            await builder.InitAsync("127.0.0.1:2379", 9999);

            _ = RunTest(builder.ServiceProvider);

            while (true)
            {
                await Task.Delay(1000);
            }
        }
    }
}
