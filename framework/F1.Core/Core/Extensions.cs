using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using F1.Abstractions;
using F1.Abstractions.Network;
using F1.Abstractions.Actor;
using F1.Abstractions.RPC;
using F1.Abstractions.Placement;
using F1.Core.Network;
using F1.Core.Message;
using F1.Core.Placement;
using F1.Core.Utils;
using F1.Core.RPC;
using F1.Core.Gateway;
using F1.Core.Actor;

namespace F1.Core.Core
{
    public static class Extensions
    {
        public static void AddDefaultServices(this IServiceBuilder builder)
        {
            AssemblyLoader.LoadAssemblies();
            MessageExt.Load();

            var services = builder.ServiceCollection;

            services.AddOptions();
            services.AddLogging();

            services.TryAddSingleton<IConnectionManager, ConnectionManager>();
            services.TryAddSingleton<IConnectionSessionInfoFactory, DefaultConnectionSessionInfoFactory>();
            services.TryAddSingleton<IClientConnectionFactory, ClientConnectionFactory>();
            services.TryAddSingleton<IConnectionListener, ConnectionListener>();
            services.TryAddSingleton<IMessageCenter, MessageCenter>();
            services.TryAddSingleton<IMessageHandlerFactory, MessageHandlerFactory>();
            services.TryAddSingleton<IPlacement, PDPlacement>();
            services.TryAddSingleton<TimeBasedSequence>();
            services.TryAddSingleton<SessionUniqueSequence>();
            services.TryAddSingleton<RpcMetaData>();
            services.TryAddSingleton<RpcDispatchProxyFactory>();
            services.TryAddSingleton<DispatchHandler>();
            services.TryAddSingleton<IParametersSerializerFactory, ParametersSerializerFactory>();
            services.TryAddSingleton<TaskCompletionSourceManager>();
            services.TryAddSingleton<ClientConnectionPool>();
            services.TryAddSingleton<GatewayClientFactory>();
            services.TryAddSingleton<RpcClientFactory>();
            services.TryAddSingleton<ActorFactory>();
            services.TryAddSingleton<ActorManager>();
            services.TryAddSingleton<IActorClientFactory, ActorClientFactory>();
            services.TryAddSingleton<ActorRuntime>();
            services.TryAddSingleton<SendingThreads>();
            services.TryAddSingleton<PlacementExtension>();
        }

        public static void AddLog(this IServiceBuilder serviceBuilder, LogLevel logLevel = LogLevel.Information) 
        {
            var services = serviceBuilder.ServiceCollection;

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(logLevel);
                builder.AddNLog();
            });
        }

        public static ServiceBuilder Configure<T>(this ServiceBuilder builder, Action<T> action) where T : class
        {
            builder.ServiceCollection.Configure<T>(action);
            return builder;
        }
    }
}
