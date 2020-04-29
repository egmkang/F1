using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace F1.Core.Actor
{
    internal class ActorFactory
    {
        private Dictionary<Type, Func<Actor>> ActorConstructor = new Dictionary<Type, Func<Actor>>();
        private readonly Dictionary<Type, Func<Actor>> ActorConStuctorCache = new Dictionary<Type, Func<Actor>>();
        private readonly ILogger logger;

        public ActorFactory(ILoggerFactory loggerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
        }

        private Func<Actor> GetConstructor(Type t)
        {
            if (ActorConstructor.TryGetValue(t, out var v)) return v;

            v = Expression.Lambda<Func<Actor>>(Expression.New(t.GetConstructor(Type.EmptyTypes))).Compile();
            ActorConStuctorCache.TryAdd(t, v);
            ActorConstructor = new Dictionary<Type, Func<Actor>>(ActorConStuctorCache);
            return v;
        }

        public Actor CreateActor(Type type, string id) 
        {
            var constractor = this.GetConstructor(type);
            Contract.Assert(constractor != null);

            var actor = constractor();
            var context = new ActorContext(actor, this.logger);
            actor.InitActor(type, id, context);
            actor.Logger = this.logger;

            //TODO:
            //run

            return actor;
        }
    }
}
