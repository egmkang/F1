using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using F1.Abstractions;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Core;
using F1.Core.Message;
using F1.Core.Network;
using F1.Core.Utils;

namespace PlacementTest
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var builder = new ServiceBuilder();
            builder.AddDefaultServices();
            builder.ServiceCollection.AddLogging(opt => opt.AddConsole());

            builder.Build();

            var logFactory = builder.ServiceProvider.GetRequiredService<ILoggerFactory>();

            var logger = logFactory.CreateLogger("t");

            logger.LogInformation("1212");

            var placement = builder.ServiceProvider.GetRequiredService<IPlacement>();
            placement.SetPlacementServerInfo("127.0.0.1:2379");

            var version = await placement.GetVersionAsync();
            logger.LogInformation("{0}, {1}", version.Version, version.LastHeartBeatTime);


            logger.LogInformation("sequence Y:{0}", await placement.GenerateNewSequenceAsync("y", 100));
            logger.LogInformation("sequence Y:{0}", await placement.GenerateNewSequenceAsync("y", 100));
            logger.LogInformation("sequence Y:{0}", await placement.GenerateNewSequenceAsync("y", 100));

            logger.LogInformation("sequence X:{0}", await placement.GenerateNewSequenceAsync("x", 100));
            logger.LogInformation("sequence X:{0}", await placement.GenerateNewSequenceAsync("x", 100));
            logger.LogInformation("sequence X:{0}", await placement.GenerateNewSequenceAsync("x", 100));

            logger.LogInformation("new server:{0}", await placement.GenerateServerIDAsync());
            logger.LogInformation("new server:{0}", await placement.GenerateServerIDAsync());
            logger.LogInformation("new server:{0}", await placement.GenerateServerIDAsync());
            logger.LogInformation("new server:{0}", await placement.GenerateServerIDAsync());


            logger.LogInformation("new token:{0}", await placement.GenerateNewTokenAsync());
            logger.LogInformation("new token:{0}", await placement.GenerateNewTokenAsync());
            logger.LogInformation("new token:{0}", await placement.GenerateNewTokenAsync());
            logger.LogInformation("new token:{0}", await placement.GenerateNewTokenAsync());


            logger.LogInformation("register server");

            logger.LogInformation("local ip:{0}", Platform.GetLocalAddresss());

            var server_id = await placement.GenerateServerIDAsync();
            var server_info = new PlacementActorHostInfo();
            server_info.ServerID = server_id;
            server_info.StartTime = 101001;
            server_info.Address = Platform.GetLocalAddresss() + ":1111";
            server_info.Services.TryAdd("test_actor", "test_actor_impl");

            var lease_id = await placement.RegisterServerAsync(server_info);
            logger.LogInformation("register server response, LeaseID:{0}", lease_id);


            placement.RegisterServerChangedEvent(
                (add) => 
                {
                    logger.LogInformation("add : {0}", add.ServerID);
                },
                (remove) => 
                {
                    logger.LogInformation("remove: : {0}", remove.ServerID);
                },
                (offline) =>
                {
                    logger.LogInformation("offline : {0}", offline.ServerID);
                });

            _ = placement.StartPullingAsync();
            int count = 0;
            while (count < 20) 
            {
                await Task.Delay(1000);
                count++;
            }

            _ = placement.StopPullingAsync();
            Console.WriteLine("Hello World!");
        }
    }
}
