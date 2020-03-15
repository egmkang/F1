using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Codecs;
using F1.Abstractions.Network;
using DotNetty.Transport.Libuv;
using DotNetty.Buffers;
using System.Net;
using DotNetty.Handlers.Timeout;
using System.Runtime.InteropServices;
using F1.Core.Utils;

namespace F1.Core.Network
{
    public sealed class ConnectionListener : IConnectionListener
    {
        private IEventLoopGroup bossGroup;
        private IEventLoopGroup workGroup;
        private NetworkConfiguration config;
        private readonly ILogger logger;
        private readonly IConnectionManager connectionManager;
        private readonly IConnectionSessionInfoFactory channelSessionInfoFactory;
        private readonly List<ServerBootstrap> ports = new List<ServerBootstrap>();

        public IServiceProvider ServiceProvider { get; private set; }

        public ConnectionListener(IServiceProvider provider,
            ILoggerFactory loggerFactory,
            IConnectionManager connectionManager,
            IConnectionSessionInfoFactory channelSessionInfoFactory)
        {
            this.connectionManager = connectionManager;
            this.channelSessionInfoFactory = channelSessionInfoFactory;
            this.ServiceProvider = provider;
            this.logger = loggerFactory.CreateLogger("F1.Sockets");
        }

        public void Init(NetworkConfiguration config)
        {
            this.config = config;
            var dispatcher = new DispatcherEventLoopGroup();
            bossGroup = dispatcher;
            workGroup = new WorkerEventLoopGroup(dispatcher, config.EventLoopCount);
        }

        public async Task BindAsync(int port, IMessageHandlerFactory factory)
        {
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(this.bossGroup, this.workGroup)
                .Channel<TcpServerChannel>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                bootstrap
                    .Option(ChannelOption.SoReuseport, true)
                    .ChildOption(ChannelOption.SoReuseaddr, true);
            }

            bootstrap
                .Option(ChannelOption.SoBacklog, this.config.SoBackLog)
                .Option(ChannelOption.SoRcvbuf, this.config.RecvWindowSize)
                .Option(ChannelOption.SoSndbuf, this.config.SendWindowSize)
                .Option(ChannelOption.WriteBufferHighWaterMark, this.config.WriteBufferHighWaterMark)
                .Option(ChannelOption.WriteBufferLowWaterMark, this.config.WriteBufferLowWaterMark)
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.SoReuseaddr, true)
                .Option(ChannelOption.SoKeepalive, true)
                .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    var info = this.channelSessionInfoFactory.NewSessionInfo(factory);
                    channel.GetAttribute(ChannelExt.SESSION_INFO).Set(info);

                    var localPort = (channel.LocalAddress as IPEndPoint).Port;

                    if (channel.RemoteAddress is IPEndPoint)
                    {
                        info.RemoteAddress = channel.RemoteAddress as IPEndPoint;
                    }

                    this.connectionManager.AddConnection(channel);
                    info.RunSendingLoopAsync(channel);

                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast("TimeOut", new IdleStateHandler(this.config.ReadTimeout, this.config.WriteTimeout, this.config.ReadTimeout));
                    pipeline.AddLast(factory.NewHandler());

                    logger.LogInformation("NewSession SessionID:{0} IpAddr:{1}", info.SessionID, info.RemoteAddress?.ToString());
                }));

            await bootstrap.BindAsync(port);
            ports.Add(bootstrap);
            logger.LogInformation("Listen Port:{0}, {1}", port, factory.NewHandler().GetType());
        }


        public async Task ShutdDownAsync()
        {
            if (this.bossGroup != null) await this.bossGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5));
            if(this.workGroup != null) await this.workGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5));
        }
    }
}
