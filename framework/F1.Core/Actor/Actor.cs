using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.Actor;

namespace F1.Core.Actor
{
    public abstract class Actor : IActor
    {
        private readonly Type type;
        private readonly string id;

        public Actor(Type t, string id) 
        {
            this.type = t;
            this.id = id;
        }

        public Type ActorType => this.type;
        public string ID => this.id;
        public IActorContext Context { get; internal set; }

        protected abstract void On();
        protected abstract void Off();
    }
}
