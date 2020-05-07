using System;
using System.Threading.Tasks;
using Xunit;
using F1.Core.RPC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace F1.UnitTest.RPC
{
    [Rpc]
    public interface IRpcRquestDispatchHandlerTest
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

    public class RpcRequestDispatchHandlerTestImpl : IRpcRquestDispatchHandlerTest
    {
        private int Value { get; set; }
        public RpcRequestDispatchHandlerTestImpl(int v)
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

    public class RpcRequestDisptachHandlerTest
    {
        IServiceProvider Provider;

        public RpcRequestDisptachHandlerTest() 
        {
            var service = new ServiceCollection();
            service
                .AddSingleton<RpcMetadata>()
                .AddSingleton<IRpcRequestDispatchHandler, DispatchHandler>()
                .AddLogging(j => j.AddConsole());
            var provider = service.BuildServiceProvider();

            Provider = provider;
        }

        [Fact]
        public async Task NormalCall()
        {
            var instance1 = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcRequestDispatchHandler>();
            var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.ReturnInt", instance1, new object[0]);

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(result, 100);

            var instance2 = new RpcRequestDispatchHandlerTestImpl(101);
            var returnValue2 = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.ReturnInt", instance2, new object[0]);
            var result2 = await returnValue2.GetReturnValueAsync();
            Assert.Equal(result2, 101);
        }

        [Fact]
        public async Task DelayCall()
        {
            var instance = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcRequestDispatchHandler>();
            var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.SleepAddInt", instance, new object[1] { 100 });

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(result, 200);
        }

        [Fact]
        public async Task MethodNotFoundCall()
        {
            var instance = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcRequestDispatchHandler>();

            Assert.Throws<RpcDispatchException>(() =>
            {
                var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.SleepAddInt1", instance, new object[1] { 100 });
            });
        }

        [Fact]
        public async Task ReturnNothingCall() 
        {
            var instance = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcRequestDispatchHandler>();
            var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.ReturnNothing", instance, new object[0]);

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(result, null);
        }
    }
}
