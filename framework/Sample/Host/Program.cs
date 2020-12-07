using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using F1.Core.Config;
using F1.Core.Core;
using F1.Abstractions.Network;

namespace F1.Host
{
    class Program
    {
        static async Task  Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                                 .AddJsonFile("config.json")
                                 .Build();

            var builder = new ServiceBuilder();

            builder.Configure<HostConfiguration>((config) => configuration.GetSection("Host").Bind(config))
                    .Configure<NetworkConfiguration>((config) => configuration.GetSection("Network").Bind(config));

            builder.AddDefaultServices();
            builder.AddLog();

            builder.Build();

            var network = builder.ServiceProvider.GetRequiredService<IOptionsMonitor<NetworkConfiguration>>().CurrentValue;

            await builder.RunHostAsync();

            while (true) 
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
