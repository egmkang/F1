using System;
using System.Threading.Tasks;
using F1.Core.Config;
using F1.Core.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

            builder.ServiceCollection.AddOptions()
                   .Configure<HostConfiguration>((config) =>
                   {
                       configuration.GetSection("Host").Bind(config);
                   });

            builder.AddDefaultServices();
            builder.AddLog();

            builder.Build();

            var config = builder.ServiceProvider.GetRequiredService<IOptionsSnapshot<HostConfiguration>>();

            await builder.InitAsync("127.0.0.1:2379", 20010).ConfigureAwait(false);

            while (true) 
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
