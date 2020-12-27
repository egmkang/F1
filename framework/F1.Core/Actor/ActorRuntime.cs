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
        private static readonly DateTime RelativeTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public IServiceProvider ServiceProvider { get; internal set; }
        public long ServerID { get; internal set; }
        public TimeBasedSequence TimeSequence { get; internal set; }
        public SessionUniqueSequence SessionSequence { get; internal set; }
        private readonly ILogger logger;
        private readonly IPlacement placement;
        private readonly RpcMetaData rpcMetadata;

        public IActorContext ClientSideContext { get; internal set; }

        #region ServerSequenceContext
        private class ClientSequenceContext : IActorContext
        {
            public bool Loaded => throw new NotImplementedException();

            public string ReentrantId
            {
                get
                {
                    return $"{this.ServerID}_{this.UniqueSequence.GetNewSequence()}";
                }
                set 
                {
                    throw new Exception("cannot set ClientSideContext");
                }
            }
            public long LastMessageTime => throw new NotImplementedException();

            public long RunningLoopID => throw new NotImplementedException();

            private long ServerID;
            private readonly TimeBasedSequence UniqueSequence;
            public ClientSequenceContext(long ServerID, TimeBasedSequence uniqueSequence)
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
                            IPlacement placement,
                            RpcMetaData rpcMetadata,
                            ILoggerFactory loggerFactory,
                            TimeBasedSequence timeBasedSequence,
                            SessionUniqueSequence sessionUniqueSequence) 
        {
            this.ServiceProvider = serviceProvider;
            this.placement = placement;
            this.rpcMetadata = rpcMetadata;
            this.TimeSequence = timeBasedSequence;
            this.SessionSequence = sessionUniqueSequence;

            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
        }


        private async Task Listen(int port) 
        {
            var connectionListener = this.ServiceProvider.GetRequiredService<IConnectionListener>();
            var messageCenter = this.ServiceProvider.GetRequiredService<IMessageCenter>();
            var messageHandlerFactory = this.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();
            var clientFactory = this.ServiceProvider.GetRequiredService<IClientConnectionFactory>();
            messageHandlerFactory.Codec = new RpcMessageCodec();

            //TODO, RPC请求快速失败
            messageCenter.RegsiterEvent(
                (channel) =>
                {
                    logger.LogError("Channel Closed, SessionID:{0}", channel.GetSessionInfo().SessionID);
                },
                (outboundMessage) =>
                {
                    logger.LogError("Message Dropped, Dest SessionID:{0}", outboundMessage.DestConnection.GetSessionInfo().SessionID);
                });

            messageCenter.RegisterDefaultMessageProc(
                (inboundMessage) =>
                {
                    logger.LogWarning("Message Dropped, MessageName:{0} not find a processor", inboundMessage.MessageName);
                });

            clientFactory.Init();

            connectionListener.Init();
            await connectionListener.BindAsync(port, messageHandlerFactory).ConfigureAwait(false);
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
                this.ServerID = await this.placement.GenerateServerIDAsync().ConfigureAwait(false);

                this.SessionSequence.SetServerID(this.ServerID);
                this.TimeSequence.SetTime(Platform.GetRelativeSeconds(RelativeTime));

                this.logger.LogInformation("ActorHost ServerID:{0}", this.ServerID);

                this.ClientSideContext = new ClientSequenceContext(this.ServerID, this.TimeSequence);

                actorManager.ClientSideContext = this.ClientSideContext;
            }
            catch (Exception e) 
            {
                this.logger.LogCritical("Init ActorHost fail. Exception:{0}", e.ToString());
            }

            try
            {
                await this.Listen(port).ConfigureAwait(false);
            }
            catch (Exception e) 
            {
                this.logger.LogCritical("Listen Port:{0} fail, Exception:{1}", port, e);
            }

            try
            {
                var server_info = new PlacementActorHostInfo();
                server_info.ServerID = this.ServerID;
                server_info.StartTime = Platform.GetMilliSeconds();
                server_info.Address = $"{Platform.GetLocalAddresss()}:{port}";

                //当前服务器只需要注册自己内实现的接口
                //这样PD上就有全部的接口和实现信息
                var serverTypes = this.rpcMetadata.RpcServerTypes;
                foreach (var (key, value) in serverTypes)
                {
                    if (value == null) continue;
                    server_info.Services.Add(new ActorServiceInfo
                    {
                        ActorType = key,
                        ImplType = value.Name,
                    });
                    logger.LogTrace("Register ServiceType:{0} => {1}", key, value.Name);
                }

                var lease_id = await placement.RegisterServerAsync(server_info).ConfigureAwait(false);
                logger.LogInformation("Register ServerID:{0}, LeaseID:{1}", this.ServerID, lease_id);

                _ = placement.StartPullingAsync();
            }
            catch (Exception e)
            {
                this.logger.LogCritical("Register PlacementDriver Fail. Exception:{0}", e.ToString());
            }

        }
    }
}
