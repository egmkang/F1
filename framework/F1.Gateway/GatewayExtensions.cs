using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using F1.Abstractions.Network;
using F1.Core.Config;
using F1.Core.Core;
using F1.Abstractions.Actor.Gateway;

namespace F1.Gateway
{
    public class GatewayPlayerSessionInfo 
    {
        public string PlayerID;
        public byte[] Token;
    }

    public static partial class GatewayExtensions
    {
        private const string KeyGatewaySessionInfo = "KeyGatewaySessionInfo";

        public static GatewayPlayerSessionInfo GetPlayerInfo(this IConnectionSessionInfo sessionInfo) 
        {
            if (sessionInfo.States.TryGetValue(KeyGatewaySessionInfo, out var v)) 
            {
                return v as GatewayPlayerSessionInfo;
            }
            var info = new GatewayPlayerSessionInfo();
            sessionInfo.States.TryAdd(KeyGatewaySessionInfo, info);
            return info;
        }

        public static async Task RunGatewayAsync(this ServiceBuilder builder) 
        {
            var config = builder.ServiceProvider.GetRequiredService<IOptionsMonitor<GatewayConfiguration>>().CurrentValue;
            var logger = builder.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("F1.Gateway");

            logger.LogInformation("RunGatewayAsync, PlacementDriverAddress:{0}, Host ListenPort:{1}, Gateway ListenPort:{2}, ServiceTypeName:{3}",
                                    config.PlacementDriverAddress, config.ListenPort, config.GatewayPort, config.ServiceTypeName);
            await builder.InitAsync(config.PlacementDriverAddress, config.ListenPort).ConfigureAwait(false);
            await builder.ListenGateway(config.GatewayPort).ConfigureAwait(false);
        }

        public static void AddGatewayServices(this ServiceBuilder builder) 
        {
            var services = builder.ServiceCollection;


            services.TryAddSingleton<IAuthentication, Authentication>();
            services.AddSingleton<GatewayDefaultMessageHandler>();
        }
    }
}
