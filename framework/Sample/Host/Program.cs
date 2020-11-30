using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using F1.Core.Config;
using F1.Core.Core;

namespace F1.Host
{
    class Program
    {
        static async Task  Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                                 .AddJsonFile("config.json")
                                 .Build();

            var builder = new ServiceBuilder();

            builder.Configure((config) =>
            {
                configuration.GetSection("Host").Bind(config);
            });

            builder.AddDefaultServices();
            builder.AddLog();

            builder.Build();

            await builder.RunHostAsync();

            while (true) 
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
