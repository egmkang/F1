using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using F1.Abstractions.Actor;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.RPC;
using F1.Core.Utils;
using F1.Core.Message;
using F1.Core.Network;

namespace F1.Core.Actor
{
    internal class ActorRuntime
    {
        public IServiceProvider ServiceProvider { get; internal set; }
        public long ServerID { get; internal set; }
        public UniqueSequence UniqueSequence { get; internal set; }
        private readonly ILogger logger;
        private readonly IPlacement placement;
        private readonly RpcMetadata rpcMetadata;

        public IActorContext Context { get; internal set; }

        #region ServerSequenceContext
        private class ServerSequenceContext : IActorContext
        {
            public bool Loaded => throw new NotImplementedException();

            public (long ServerID, long RequestID) CurrentRequest => (this.ServerID, this.UniqueSequence.GetNewSequence());

            public long LastMessageTime => throw new NotImplementedException();

            public long RunningLoopID => throw new NotImplementedException();

            private long ServerID;
            private readonly UniqueSequence UniqueSequence;
            public ServerSequenceContext(long ServerID, UniqueSequence uniqueSequence)
            {
                this.ServerID = ServerID;
                this.UniqueSequence = uniqueSequence;
            }

            public void Run()
            {
            }

            public void SendMail(InboundMessage inboundMessage)
            {
            }

            public void Stop()
            {
            }
        }
        #endregion

        public ActorRuntime(IServiceProvider serviceProvider, 
                            UniqueSequence uniqueSequence,
                            IPlacement placement,
                            RpcMetadata rpcMetadata,
                            ILoggerFactory loggerFactory) 
        {
            this.ServiceProvider = serviceProvider;
            this.UniqueSequence = uniqueSequence;
            this.placement = placement;
            this.rpcMetadata = rpcMetadata;

            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
        }


        private async Task Listen(int port) 
        {
            var connectionListener = this.ServiceProvider.GetRequiredService<IConnectionListener>();
            var messageCenter = this.ServiceProvider.GetRequiredService<IMessageCenter>();
            var messageHandlerFactory = this.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();
            var connectionFactory = this.ServiceProvider.GetRequiredService<IClientConnectionFactory>();
            messageHandlerFactory.Codec = new ProtobufMessageCodec();

            //TODO
            //RPC请求快速失败
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
                    //TODO
                    //这边需要处理网关来的消息
                });

            connectionFactory.Init(new NetworkConfiguration() { });

            connectionListener.Init(new NetworkConfiguration() { });
            await connectionListener.BindAsync(port, messageHandlerFactory);
        }

        public async Task InitActorRuntime(int port) 
        {
            var sendThreads = this.ServiceProvider.GetRequiredService<SendingThreads>();
            //保证RequestRpc, RequestRPCHeartBeat消息被监听
            var actorManager = this.ServiceProvider.GetRequiredService<ActorManager>();
            //保证PD的事件被监听
            var rpcClientFactory = this.ServiceProvider.GetRequiredService<RpcClientFactory>();

            try
            {
                this.ServerID = await this.placement.GenerateServerIDAsync();
                this.UniqueSequence.SetHighPart(this.ServerID);
                this.logger.LogInformation("ActorHost ServerID:{0}", this.ServerID);

                this.Context = new ServerSequenceContext(this.ServerID, this.UniqueSequence);
            }
            catch (Exception e) 
            {
                this.logger.LogCritical("Init ActorHost fail. Exception:{0}", e.ToString());
            }

            try
            {
                await this.Listen(port);
            }
            catch (Exception e) 
            {
                this.logger.LogCritical("Listen Port:{0} fail", port);
            }

            try
            {
                var server_info = new PlacementActorHostInfo();
                server_info.ServerID = this.ServerID;
                server_info.Domain = "t";
                server_info.StartTime = Platform.GetMilliSeconds();
                server_info.Address = $"{Platform.GetLocalAddresss()}:{port}";

                var serverTypes = this.rpcMetadata.RpcServerTypes;
                foreach (var (key, _) in serverTypes)
                {
                    server_info.ActorType.Add(key);
                    logger.LogTrace("Register ServiceType:{0}", key);
                }

                var lease_id = await placement.RegisterServerAsync(server_info);
                logger.LogInformation("Register ServerID:{1}, LeaseID:{0}", this.ServerID, lease_id);

                _ = placement.StartPullingAsync();
            }
            catch (Exception e)
            {
                this.logger.LogCritical("Register PlacementDriver Fail. Exception:{0}", e.ToString());
            }

        }
    }
}
