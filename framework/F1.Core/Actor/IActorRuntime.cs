using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.Actor
{
    internal interface IActorRuntime
    {
        long ServerID { get; }

        IActorFactory ActorFactory { get; }

        IServiceProvider ServiceProvider { get; }
    }
}
