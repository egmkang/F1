using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using F1.Abstractions;
using F1.Core.Config;

namespace F1.Core.Core
{
    public static class HostExtensions
    {
        public static async Task RunHostAsync(this ServiceBuilder builder) 
        {
            var config = builder.ServiceProvider.GetRequiredService<IOptionsMonitor<HostConfiguration>>().CurrentValue;
            var logger = builder.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("F1.Core");

            logger.LogInformation("RunHostAsync, PlacementDriverAddress:{0}, Host ListenPort:{1}",
                                    config.PlacementDriverAddress, config.ListenPort);

            await builder.InitAsync(config.PlacementDriverAddress, config.ListenPort).ConfigureAwait(false);
        }
    }
}
