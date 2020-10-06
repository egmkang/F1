using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Network;
using F1.Core.Core;
using F1.Core.Message;

namespace F1.Gateway
{
    public static class GatewayListener
    {
        private static readonly IMessageCodec codec = new BlockMessageCodec();

        public static async Task ListenGateway(this ServiceBuilder builder, int port) 
        {
            var serviceProvider = builder.ServiceProvider;

            var logFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = logFactory.CreateLogger("F1.Gateway");

            try
            {
                var connectionListener = serviceProvider.GetRequiredService<IConnectionListener>();
                var messageCenter = serviceProvider.GetRequiredService<IMessageCenter>();

                var messageHandlerFactory = new MessageHandlerFactory(serviceProvider, logFactory, messageCenter);
                messageHandlerFactory.Codec = codec;

                var gatewayMessageHandler = serviceProvider.GetRequiredService<GatewayDefaultMessageHandler>();

                await connectionListener.BindAsync(port, messageHandlerFactory).ConfigureAwait(false);
                logger.LogInformation("ListenGateway success, Port:{0}", port);
            }
            catch (Exception e) 
            {
                logger.LogCritical("ListenGateway fail, Port:{0}, Exception:{1}", port, e);
            }
        }
    }
}
