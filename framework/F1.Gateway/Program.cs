using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using F1.Core.Core;
using F1.Gateway.Actor;
using F1.Abstractions.Abstractions.Gateway;

namespace F1.Gateway
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ServiceBuilder();

            builder.AddDefaultServices();
            builder.AddGatewayServices();
            builder.AddLog();

            builder.Build();

            await builder.InitAsync("127.0.0.1:2379", 20001).ConfigureAwait(false);
            await builder.ListenGateway(20000).ConfigureAwait(false);

            while (true) 
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
