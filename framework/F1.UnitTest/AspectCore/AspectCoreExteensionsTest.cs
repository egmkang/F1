using System;
using System.Threading.Tasks;
using Xunit;
using F1.Core.RPC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace F1.UnitTest.AspectCore
{
    [Rpc]
    public interface ITest 
    {
        Task<int> ReturnInt();

        Task<int> SleepAddInt(int x);

        Task ReturnNothing();
    }

    [Rpc]
    public interface ITest1
    {
        Task<int> ReturnInt();
    }

    public class DemoServer : ITest
    {
        private int Value { get; set; }
        public DemoServer(int v)
        {
            Value = v;
        }

        public Task<int> ReturnInt()
        {
            return Task.FromResult(Value);
        }

        public Task ReturnNothing()
        {
            return Task.CompletedTask;
        }

        public async Task<int> SleepAddInt(int x)
        {
            await Task.Delay(10);
            return Value + x;
        }
    }

    public class AspectCoreExtensionsTest
    {
        IServiceProvider Provider;

        public AspectCoreExtensionsTest() 
        {
            var service = new ServiceCollection();
            service
                .AddSingleton<IAspectCoreExtensions, AspectCoreExtensions>()
                .AddLogging(j => j.AddConsole());
            var provider = service.BuildServiceProvider();

            Provider = provider;
        }

        [Fact]
        public void GetClientAndTypes()
        {
            var aspectCore = Provider.GetRequiredService<IAspectCoreExtensions>();
            var t = aspectCore.GetServerType(typeof(ITest).Name);

            Assert.Equal(t, typeof(DemoServer));

            t = aspectCore.GetClientType(typeof(ITest).Name);
            Assert.Equal(t, typeof(ITest));

            t = aspectCore.GetServerType(typeof(ITest1).Name);
            Assert.Null(t);

            t = aspectCore.GetClientType(typeof(ITest1).Name);
            Assert.Equal(t, typeof(ITest1));
        }

        [Fact]
        public async Task NormalCall()
        {
            var instance1 = new DemoServer(100);

            var aspectCore = Provider.GetService<IAspectCoreExtensions>();
            var returnValue = aspectCore.Invoke($"{typeof(ITest).Name}.ReturnInt", instance1, new object[0]);

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(result, 100);

            var instance2 = new DemoServer(101);
            var returnValue2 = aspectCore.Invoke($"{typeof(ITest).Name}.ReturnInt", instance2, new object[0]);
            var result2 = await returnValue2.GetReturnValueAsync();
            Assert.Equal(result2, 101);
        }

        [Fact]
        public async Task DelayCall()
        {
            var instance = new DemoServer(100);

            var aspectCore = Provider.GetService<IAspectCoreExtensions>();
            var returnValue = aspectCore.Invoke($"{typeof(ITest).Name}.SleepAddInt", instance, new object[1] { 100 });

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(result, 200);
        }

        [Fact]
        public async Task MethodNotFoundCall()
        {
            var instance = new DemoServer(100);

            var aspectCore = Provider.GetService<IAspectCoreExtensions>();

            Assert.Throws<AspectExtensionException>(() =>
            {
                var returnValue = aspectCore.Invoke($"{typeof(ITest).Name}.SleepAddInt1", instance, new object[1] { 100 });
            });
        }

        [Fact]
        public async Task ReturnNothingCall() 
        {
            var instance = new DemoServer(100);

            var aspectCore = Provider.GetService<IAspectCoreExtensions>();
            var returnValue = aspectCore.Invoke($"{typeof(ITest).Name}.ReturnNothing", instance, new object[0]);

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(result, null);
        }
    }
}
