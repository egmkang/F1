using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using F1.Core.Core;

namespace F1.Gateway
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var builder = new ServiceBuilder();
            builder.AddDefaultServices();
            builder.ServiceCollection.AddSingleton<GatewayMessageHandler>();
            builder.ServiceCollection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddNLog();
            });
            builder.Build();

            await builder.InitAsync("127.0.0.1:2379", 20001);
            await builder.ListenGateway(20000);

            while (true) 
            {
                await Task.Delay(1000);
            }
        }
    }
}
