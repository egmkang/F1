using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using F1.Core.Core;
using F1.Sample.Impl;
using F1.Gateway.Actor;

namespace F1.Gateway
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Load.LoadByForce();

            var builder = new ServiceBuilder();

            builder.AddDefaultServices();
            builder.AddGatewayServices();
            builder.AddLog();

            builder.Build();

            //TODO
            //通过Hack的方式先绕过去
            builder.ConfigActorServices(new List<string>()
            {
                typeof(GatewayImpl).Name,
            });

            await builder.InitAsync("127.0.0.1:2379", 20001).ConfigureAwait(false);
            await builder.ListenGateway(20000).ConfigureAwait(false);

            while (true) 
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
