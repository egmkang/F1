using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using F1.Abstractions;
using F1.Core.Core;
using F1.Abstractions.Network;
using F1.Core.Message;
using F1.Core.Network;
using F1.Core.RPC;

namespace F1.UnitTest.Network
{
    public class Server
    {
        static Random random = new Random();
        static int Port = random.Next(10000, 30000);
        static NetworkConfiguration config = new NetworkConfiguration();

        IServiceBuilder MakeBuilder() 
        {
            var  builder = new ServiceBuilder();
            builder.AddDefaultServices();

            builder.Build();

            return builder;
        }

        [Fact]
        public void ServiceProvider() 
        {
            var builder = MakeBuilder();

            var logFactory = builder.ServiceProvider.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logFactory);

            var logger = logFactory.CreateLogger("test");
            logger.LogInformation("1111");

            var connectionManager = builder.ServiceProvider.GetRequiredService<IConnectionManager>();
            Assert.NotNull(connectionManager);

            var connectionSessionInfoFactory = builder.ServiceProvider.GetRequiredService<IConnectionSessionInfoFactory>();
            Assert.NotNull(connectionSessionInfoFactory);

            var connectionFactory = builder.ServiceProvider.GetRequiredService<IClientConnectionFactory>();
            Assert.NotNull(connectionFactory);

            var connectionListener = builder.ServiceProvider.GetRequiredService<IConnectionListener>();
            Assert.NotNull(connectionListener);

            var messageCenter = builder.ServiceProvider.GetRequiredService<IMessageCenter>();
            Assert.NotNull(messageCenter);


            var messageHandlerFactory = builder.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();
            Assert.NotNull(messageHandlerFactory);


            builder.ShutDown();
        }

        [Fact]
        public void EchoServer() 
        {
            var builder = MakeBuilder();

            var logFactory = builder.ServiceProvider.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logFactory);

            var connectionManager = builder.ServiceProvider.GetRequiredService<IConnectionManager>();
            Assert.NotNull(connectionManager);

            var connectionSessionInfoFactory = builder.ServiceProvider.GetRequiredService<IConnectionSessionInfoFactory>();
            Assert.NotNull(connectionSessionInfoFactory);

            var connectionFactory = builder.ServiceProvider.GetRequiredService<IClientConnectionFactory>();
            Assert.NotNull(connectionFactory);

            var connectionListener = builder.ServiceProvider.GetRequiredService<IConnectionListener>();
            Assert.NotNull(connectionListener);

            var messageCenter = builder.ServiceProvider.GetRequiredService<IMessageCenter>();
            Assert.NotNull(messageCenter);


            var messageHandlerFactory = builder.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();
            Assert.NotNull(messageHandlerFactory);

            messageHandlerFactory.Codec = new StringMessageCodec();


            var count = 0;

            messageCenter.RegsiterEvent(
                (channel) =>
                {
                    Console.WriteLine("channel closed");
                },
                (outboundMessage) =>
                {
                    Console.WriteLine("message dropped");
                });
            messageCenter.RegisterDefaultMessageProc(
                (inboundMessage) =>
                {
                    var channel = inboundMessage.SourceConnection;
                    var outboundMessage = new OutboundMessage(channel, inboundMessage.Inner);
                    messageCenter.SendMessage(outboundMessage);
                    count++;
                });

            connectionListener.Init(config);
            connectionListener.BindAsync(Port, messageHandlerFactory);


            connectionFactory.Init(config);
            var client = connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port), messageHandlerFactory);
            client.ContinueWith((v) =>
            {
                var outboundMessage = new OutboundMessage(client.Result, "1212121asasa212");
                var sessionInfo = client.Result.GetSessionInfo();
                sessionInfo.PutOutboundMessage(outboundMessage);
            });

            Thread.Sleep(1000);

            client.Result.CloseAsync();

            Assert.True(count >= 2);

            builder.ShutDown();
        }
    }
}
