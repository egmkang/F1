using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using F1.Core.Core;
using F1.Gateway.Actor;
using F1.Abstractions.Abstractions.Gateway;
using F1.Gateway;
using F1.Core.Config;

namespace Sample.Gateway
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                                 .AddJsonFile("config.json")
                                 .Build();

            var builder = new ServiceBuilder();

            builder.Configure<GatewayConfiguration>((config) =>
            {
                configuration.GetSection("Gateway").Bind(config);
            });

            builder.AddDefaultServices();
            builder.AddGatewayServices();
            builder.AddLog();

            builder.Build();

            await builder.RunGatewayAsync();

            while (true) 
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
