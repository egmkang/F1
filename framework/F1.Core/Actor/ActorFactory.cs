using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using System.Text;
using F1.Core.RPC;

namespace F1.Core.Actor
{
    internal class ActorFactory
    {
        private Dictionary<Type, Func<Actor>> ActorConstructor = new Dictionary<Type, Func<Actor>>();
        private readonly object mutex = new object();
        private readonly Dictionary<Type, Func<Actor>> ActorConStuctorCache = new Dictionary<Type, Func<Actor>>();
        private readonly ILogger logger;
        private readonly RpcDispatchProxyFactory proxyFactory;
        private readonly DispatchHandler requestDispatchHandler;

        public ActorFactory(ILoggerFactory loggerFactory, RpcDispatchProxyFactory proxyFactory, DispatchHandler requestDispatchHandler) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.Actor");
            this.proxyFactory = proxyFactory;
            this.requestDispatchHandler = requestDispatchHandler;
        }

        private Func<Actor> GetConstructor(Type t)
        {
            if (ActorConstructor.TryGetValue(t, out var v)) return v;

            lock (mutex) 
            {
                v = Expression.Lambda<Func<Actor>>(Expression.New(t.GetConstructor(Type.EmptyTypes))).Compile();
                ActorConStuctorCache.TryAdd(t, v);
                ActorConstructor = new Dictionary<Type, Func<Actor>>(ActorConStuctorCache);
            }
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
            actor.ProxyFactory = this.proxyFactory;
            context.Dispatcher = this.requestDispatchHandler;

            this.logger.LogInformation("CreateActor, ID:{0}", actor.UniqueID);

            context.Run();
            return actor;
        }
    }
}
