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
        private readonly RpcMetadata rpcMetadata;
        private readonly List<string> services = new List<string>();

        public IActorContext Context { get; internal set; }

        #region ServerSequenceContext
        private class ServerSequenceContext : IActorContext
        {
            public bool Loaded => throw new NotImplementedException();

            public (long ServerID, long RequestID) CurrentRequest => (this.ServerID, this.UniqueSequence.GetNewSequence());

            public long LastMessageTime => throw new NotImplementedException();

            public long RunningLoopID => throw new NotImplementedException();

            private long ServerID;
            private readonly TimeBasedSequence UniqueSequence;
            public ServerSequenceContext(long ServerID, TimeBasedSequence uniqueSequence)
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
                            RpcMetadata rpcMetadata,
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
            messageHandlerFactory.Codec = new ProtobufMessageCodec();

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

            clientFactory.Init(new NetworkConfiguration() { });

            connectionListener.Init(new NetworkConfiguration() { });
            await connectionListener.BindAsync(port, messageHandlerFactory).ConfigureAwait(false);
        }

        public void ConfigActorServices(List<string> svc) 
        {
            this.services.AddRange(svc);
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

                this.Context = new ServerSequenceContext(this.ServerID, this.TimeSequence);
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

                //TODO
                //先通过这种方式绕过去
                //后面改一下PD的实现和协议, 接口和实现分离
                if (this.services.Count == 0)
                {
                    var serverTypes = this.rpcMetadata.RpcServerTypes;
                    foreach (var (key, value) in serverTypes)
                    {
                        server_info.ActorType.Add(value.Name);
                        logger.LogTrace("Register InterfaceType:{1}, ServiceType:{0}", value.Name, key);
                    }
                }
                else 
                {
                    foreach (var value in this.services) 
                    {
                        server_info.ActorType.Add(value);
                        logger.LogTrace("Register ServiceType:{0}", value);
                    }
                }

                var lease_id = await placement.RegisterServerAsync(server_info).ConfigureAwait(false);
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
