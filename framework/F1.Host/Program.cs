using System;
using System.Threading.Tasks;
using F1.Core.Core;
using F1.Sample.Impl;

namespace F1.Host
{
    class Program
    {
        static async Task  Main(string[] args)
        {
            Load.LoadByForce();

            var builder = new ServiceBuilder();

            builder.AddDefaultServices();
            builder.AddLog();

            builder.Build();

            await builder.InitAsync("127.0.0.1:2379", 20010).ConfigureAwait(false);

            while (true) 
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
