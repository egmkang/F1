using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions
{
    public interface IServiceBuilder
    {
        IServiceProvider ServiceProvider { get; }
        IServiceCollection ServiceCollection { get; }

        IServiceBuilder Build();

        void ShutDown();
    }
}
