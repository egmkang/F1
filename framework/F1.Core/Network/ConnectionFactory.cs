using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using F1.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace F1.Core.Network
{
    public class ConnectionFactory : IConnectionFactory
    {
        private MultithreadEventLoopGroup group;
        private readonly object mutex = new object();
        private Dictionary<IMessageHandlerFactory, Bootstrap> bootstraps = new Dictionary<IMessageHandlerFactory, Bootstrap>();

        private ILogger logger;
        private IConnectionManager connectionManager;
        private IConnectionSessionInfoFactory channelSessionInfoFactory;
        private NetworkConfiguration config;

        public IServiceProvider ServiceProvider { get; private set; }

        public ConnectionFactory(IServiceProvider provider,
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
            this.group = new MultithreadEventLoopGroup(this.config.EventLoopCount);
        }

        public async Task<IChannel> ConnectAsync(EndPoint address, IMessageHandlerFactory factory)
        {
            Bootstrap bootstrap;
            lock (mutex) 
            {
                if (!this.bootstraps.TryGetValue(factory, out bootstrap))
                {
                    bootstrap = new Bootstrap();
                    bootstrap
                        .Group(this.group)
                        .Channel<TcpSocketChannel>()
                        .Option(ChannelOption.TcpNodelay, true)
                        .Option(ChannelOption.SoRcvbuf, this.config.RecvWindowSize)
                        .Option(ChannelOption.SoSndbuf, this.config.SendWindowSize)
                        .Option(ChannelOption.WriteBufferHighWaterMark, this.config.WriteBufferHighWaterMark)
                        .Option(ChannelOption.WriteBufferLowWaterMark, this.config.WriteBufferLowWaterMark)
                        .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                        .Attribute(ChannelExt.SESSION_INFO, this.channelSessionInfoFactory.NewSessionInfo())
                        .Handler(new ActionChannelInitializer<IChannel>(channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                            pipeline.AddLast("TimeOut", new IdleStateHandler(this.config.ReadTimeout, this.config.WriteTimeout, this.config.ReadTimeout));
                            pipeline.AddLast(factory.NewHandler());

                            logger.LogInformation("New Client Session, SessionID:{0}", channel.GetSessionInfo().SessionID);
                        }));

                    this.bootstraps.TryAdd(factory, bootstrap);
                }
            }
            var channel = await bootstrap.ConnectAsync(address);
            this.connectionManager.AddConnection(channel);
            return channel;
        }

        public async Task ShutdDownAsync()
        {
            await this.group.ShutdownGracefullyAsync();
        }
    }
}
