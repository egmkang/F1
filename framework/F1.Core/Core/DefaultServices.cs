﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using F1.Abstractions;
using F1.Abstractions.Network;
using F1.Core.Network;
using F1.Core.Message;

namespace F1.Core.Core
{
    public static class DefaultServices
    {
        public static void AddDefaultServices(this IServiceBuilder builder)
        {
            var services = builder.ServiceCollection;

            services.AddLogging();
            services.TryAddSingleton<IConnectionManager, ConnectionManager>();
            services.TryAddSingleton<IConnectionSessionInfoFactory, DefaultConnectionSessionInfoFactory>();
            services.TryAddSingleton<IConnectionFactory, ConnectionFactory>();
            services.TryAddSingleton<IConnectionListener, ConnectionListener>();
            services.TryAddSingleton<IMessageCenter, MessageCenter>();
            services.TryAddSingleton<IMessageHandlerFactory, MessageHandlerFactory>();
        }
    }
}