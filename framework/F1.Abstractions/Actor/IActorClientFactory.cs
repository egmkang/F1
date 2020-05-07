using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions.Actor
{
    public interface IActorClientFactory
    {
        T GetActorProxy<T>(string name);
    }
}
