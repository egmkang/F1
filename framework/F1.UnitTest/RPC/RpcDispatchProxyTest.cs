using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;
using Google.Protobuf;
using RpcProto;
using F1.Abstractions.Placement;
using F1.Abstractions.RPC;
using F1.Core.RPC;
using F1.Core.Utils;


namespace F1.UnitTest.RPC
{
    [Rpc]
    public interface IDispatchProxyTest1
    {
        Task VoidFunc();
        Task<int> ReturnInt();
        Task InputInt(int a);
        Task<string> InputMany(int a, float b, string c, object d);
        Task<string[]> ReturnMany(int a, int b);
    }

    public class DispatchProxyTestImpl : IDispatchProxyTest1
    {
        public Task InputInt(int a)
        {
            return Task.CompletedTask;
        }

        public Task<string> InputMany(int a, float b, string c, object d)
        {
            var s = $"{a}.{b}.{c}.{d}";
            return Task.FromResult(s);
        }

        public Task<int> ReturnInt()
        {
            return Task.FromResult(1111);
        }

        public Task<string[]> ReturnMany(int a, int b)
        {
            return Task.FromResult(new string[] { "1111", "2222", "333" });
        }

        public Task VoidFunc()
        {
            return Task.CompletedTask;
        }
    }

    public static partial class Ext 
    {
        public static ResponseRpc MakeResp(this object msg) 
        {
            var req = msg as RequestRpc;
            return new ResponseRpc() 
            {
                 Request = req,
                 RequestId = req.RequestId,
                 ResponseId = req.ResponseId,
            };
        }
    }

    public class RpcDispatchProxyTest : Setup
    {
        IServiceProvider Provider;
        RpcDispatchProxyFactory ProxyFactory;
        IParametersSerializer Serializer;

        public RpcDispatchProxyTest()
        {
            var service = new ServiceCollection();
            service
                .AddSingleton<RpcMetaData>()
                .AddSingleton<IParametersSerializer, ParametersSerializerCeras>()
                .AddSingleton<TimeBasedSequence>()
                .AddSingleton<RpcDispatchProxyFactory>()
                .AddSingleton<UniqueSequence>()
                .AddLogging(j => j.AddConsole());
            var provider = service.BuildServiceProvider();

            Provider = provider;
            Serializer = Provider.GetRequiredService<IParametersSerializer>();
            ProxyFactory = Provider.GetRequiredService<RpcDispatchProxyFactory>();
        }

        public TrySendRpcRequestFunc MakeSimpleResponse(byte[] bytes, int delay = 0, Exception e = null)
        {
            return async (_actor, _msg, _clear) => 
            {
                await Task.Yield();
                await Task.Delay(delay);

                if (e != null)  throw e;

                var response = _msg.MakeResp();
                response.Response = ByteString.CopyFrom(bytes);

                return response;
            };
        }

        [Fact]
        public async Task NormalCall() 
        {
            var proxy = ProxyFactory.CreateProxy<IDispatchProxyTest1>("111");
            var inner = proxy as RpcDispatchProxy;
            inner.SendHook = MakeSimpleResponse(Serializer.Serialize(null, null));

            Assert.NotNull(proxy);
            await proxy.InputInt(121212);
        }

        [Fact]
        public async Task TimeoutExceptionCall()
        {
            var proxy = ProxyFactory.CreateProxy<IDispatchProxyTest1>("111");
            var inner = proxy as RpcDispatchProxy;
            inner.SendHook = MakeSimpleResponse(Serializer.Serialize(null, null), 1000, new RpcTimeOutException());

            Exception E = null;
            try
            {
                await proxy.InputInt(121212);
            }
            catch (Exception e)
            {
                E = e;
            }
            Assert.IsType<RpcTimeOutException>(E);
        }

        [Fact]
        public async Task InputManyCall()
        {
            var expected = $"{1212}.{2232}.asas.dddd";
            var proxy = ProxyFactory.CreateProxy<IDispatchProxyTest1>("111");
            var inner = proxy as RpcDispatchProxy;
            inner.SendHook = MakeSimpleResponse(Serializer.Serialize(expected, typeof(string)));

            var s = await proxy.InputMany(1212, 2232, "asas", "dddd");
            Assert.Equal(s, expected);
        }

        [Fact]
        public async Task VoidFuncCall() 
        {
            var proxy = ProxyFactory.CreateProxy<IDispatchProxyTest1>("111");
            var inner = proxy as RpcDispatchProxy;
            inner.SendHook = MakeSimpleResponse(Serializer.Serialize(null, null));

            await proxy.VoidFunc();
        }

        [Fact]
        public async Task SimpleReturnInt() 
        {
            var proxy = ProxyFactory.CreateProxy<IDispatchProxyTest1>("111");
            var inner = proxy as RpcDispatchProxy;
            inner.SendHook = MakeSimpleResponse(Serializer.Serialize(1111, typeof(int)));

            var result = await proxy.ReturnInt();
            Assert.Equal(1111, result);
        }

        string[] MakeString(int a, int b) 
        {
            var result = new string[b];
            for (int i = 0; i < b; ++i) 
            {
                result[i] = a.ToString();
            }
            return result;
        }

        [Fact]
        public async Task ReturnManyCall() 
        {
            var proxy = ProxyFactory.CreateProxy<IDispatchProxyTest1>("111");
            var inner = proxy as RpcDispatchProxy;


            var args = new (int a, int b)[] 
            {
                (1, 10),
                (10, 0),
                (1212, 2),
                (2323213, 8),
            };

            foreach (var (a, b) in args) 
            {
                var s = MakeString(a, b);
                inner.SendHook = MakeSimpleResponse(Serializer.Serialize(s, typeof(string[])));

                var result = await proxy.ReturnMany(a, b);
                Assert.Equal(s, result);
            }
        }
    }
}
