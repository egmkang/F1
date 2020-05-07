using F1.Abstractions.Actor;
using F1.Abstractions.Network;
using F1.Abstractions.Placement;
using F1.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace F1.Core.Actor
{
    internal class ActorRuntime
    {
        public IServiceProvider ServiceProvider { get; internal set; }
        public long ServerID { get; internal set; }
        public UniqueSequence UniqueSequence { get; internal set; }
        private readonly ILogger logger;
        private readonly IPlacement placement;

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
                            ILoggerFactory loggerFactory) 
        {
            this.ServiceProvider = serviceProvider;
            this.UniqueSequence = uniqueSequence;
            this.placement = placement;

            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
        }

        public async Task InitActorRuntime() 
        {
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
        }
    }
}
