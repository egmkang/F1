using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Actor;
using F1.Abstractions.Network;
using F1.Core.Utils;

namespace F1.Core.Actor
{
    internal class ActorContext : IActorContext
    {
        private readonly AsyncMessageQueue<InboundMessage> mailBox = new AsyncMessageQueue<InboundMessage>();
        private readonly ILogger logger;
        internal Actor Actor { get; set; }

        public (long ServerID, long RequestID) CurrentRequest { get; internal set; }

        public long RunningLoopID { get; internal set; }

        public long LastMessageTime { get; internal set; }

        public bool Loaded { get; internal set; }

        public ActorContext(Actor actor, ILogger logger) 
        {
            this.Actor = actor;
            this.logger = logger;

            this.LastMessageTime = Platform.GetMilliSeconds();
            this.Loaded = false;
        }

        public void SendMail(InboundMessage inboundMessage)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Run()
        {
            throw new NotImplementedException();
        }
    }
}
