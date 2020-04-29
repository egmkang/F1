using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Actor;
using F1.Abstractions.Network;

namespace F1.Core.Actor
{
    public abstract class Actor : IActor
    {
        public Type ActorType { get; private set; }
        public string ID { get; private set; }
        internal IActorContext Context { get; set; }
        public ILogger Logger { get; internal set; }

        internal void InitActor(Type type, string id, IActorContext context)
        {
            this.ActorType = type;
            this.ID = id;
            this.Context = context;
        }

        protected virtual Task OnActivateAsync() 
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnDeactivateASync() 
        {
            return Task.CompletedTask;
        }

        protected virtual Task ProcessInputMessage(InboundMessage msg) 
        {
            return Task.CompletedTask;
        }
    }
}
