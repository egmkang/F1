using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions;
using F1.Abstractions.Actor.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F1.Gateway
{
    static class GatewayServices
    {
        public static void AddGatewayServices(this IServiceBuilder builder) 
        {
            var services = builder.ServiceCollection;


            services.TryAddSingleton<IAuthentication, Authentication>();
            services.AddSingleton<GatewayDefaultMessageHandler>();
        }
    }
}
