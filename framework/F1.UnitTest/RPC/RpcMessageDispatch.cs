using System;
using System.Threading.Tasks;
using Xunit;
using F1.Core.RPC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using F1.Abstractions.RPC;


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

    public class RpcRequestDisptachHandlerTest : Setup
    {
        IServiceProvider Provider;

        public RpcRequestDisptachHandlerTest() 
        {
            var service = new ServiceCollection();
            service
                .AddSingleton<RpcMetaData>()
                .AddSingleton<IRpcDispatchHandler, DispatchHandler>()
                .AddLogging(j => j.AddConsole());
            var provider = service.BuildServiceProvider();

            Provider = provider;
        }

        [Fact]
        public async Task NormalCall()
        {
            var instance1 = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcDispatchHandler>();
            var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.ReturnInt", instance1, new object[0]);

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(100, result);

            var instance2 = new RpcRequestDispatchHandlerTestImpl(101);
            var returnValue2 = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.ReturnInt", instance2, new object[0]);
            var result2 = await returnValue2.GetReturnValueAsync();
            Assert.Equal(101, result2);
        }

        [Fact]
        public async Task DelayCall()
        {
            var instance = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcDispatchHandler>();
            var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.SleepAddInt", instance, new object[1] { 100 });

            var result = await returnValue.GetReturnValueAsync();
            Assert.Equal(200, result);
        }

        [Fact]
        public void MethodNotFoundCall()
        {
            var instance = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcDispatchHandler>();

            Assert.Throws<RpcDispatchException>(() =>
            {
                var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.SleepAddInt1", instance, new object[1] { 100 });
            });
        }

        [Fact]
        public async Task ReturnNothingCall() 
        {
            var instance = new RpcRequestDispatchHandlerTestImpl(100);

            var aspectCore = Provider.GetService<IRpcDispatchHandler>();
            var returnValue = aspectCore.Invoke($"{typeof(IRpcRquestDispatchHandlerTest).Name}.ReturnNothing", instance, new object[0]);

            var result = await returnValue.GetReturnValueAsync();
            Assert.Null(result);
        }
    }
}
