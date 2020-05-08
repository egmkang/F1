using F1.Abstractions;
using F1.Abstractions.Network;
using F1.Core.Core;
using F1.Core.Message;
using F1.Core.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Threading;

namespace sample
{
    class Program
    {
        static Random random = new Random();
        static int Port = random.Next(10000, 30000);
        static NetworkConfiguration config = new NetworkConfiguration();

        static IServiceBuilder MakeBuilder()
        {
            var builder = new ServiceBuilder();
            builder.AddDefaultServices();

            builder.Build();

            return builder;
        }

        static void Main(string[] args)
        {
            var builder = new ServiceBuilder();
            builder.AddDefaultServices();
            builder.ServiceCollection.AddLogging(opt => opt.AddConsole());

            builder.Build();

            var logFactory = builder.ServiceProvider.GetRequiredService<ILoggerFactory>();

            var logger = logFactory.CreateLogger("t");

            var connectionListener = builder.ServiceProvider.GetRequiredService<IConnectionListener>();

            var messageCenter = builder.ServiceProvider.GetRequiredService<IMessageCenter>();

            var messageHandlerFactory = builder.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();

            messageHandlerFactory.Codec = new StringMessageCodec();


            var count = 0;

            messageCenter.RegsiterEvent(
                (channel) =>
                {
                    logger.LogError("Channel Closed, SessionID:{0}", channel.GetSessionInfo().SessionID);
                },
                (outboundMessage) =>
                {
                    logger.LogError("Message Dropped, Dest SessionID:{0}", outboundMessage.DestConnection.GetSessionInfo().SessionID);
                });

            messageCenter.RegisterMessageProc("",
                (inboundMessage) =>
                {
                    var channel = inboundMessage.SourceConnection;
                    var outboundMessage = new OutboundMessage(channel, inboundMessage.Inner);
                    messageCenter.SendMessage(outboundMessage);
                    count++;
                });

            connectionListener.Init(config);
            connectionListener.BindAsync(Port, messageHandlerFactory);


            while (true)
            {
                Thread.Sleep(100);
            }

            builder.ShutDown();
        }
    }
}
