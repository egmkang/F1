using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AspectCore.Extensions.Reflection;
using F1.Core.Utils;
using F1.Abstractions.RPC;
using F1.Abstractions.Actor;
using F1.Abstractions.Placement;

namespace F1.Core.RPC
{
    internal class RequestDisptachProxyClientHandler
    {
        public RequestDisptachProxyClientHandler(Func<IGenericCompletionSource> f,
            bool isOneWay,
            string name,
            Type[] paramsType,
            Type returnType) 
        {
            this.NewCompletionSource = f;
            this.IsOneWay = isOneWay;
            this.Name = name;
            this.ParametersType = paramsType;
            this.ReturnType = returnType;
        }

        public Func<IGenericCompletionSource> NewCompletionSource { get; private set; }
        public bool IsOneWay { get; private set; }
        public string Name { get; private set; }
        public Type ReturnType { get; private set; }
        public Type[] ParametersType { get; private set; }
    }

    public class RpcDispatchProxyFactory
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly RpcMetadata metadata;
        private TimeBasedSequence timeBasedSequence;
        private RpcClientFactory rpcClientFactory;
        private readonly IParametersSerializer serializer;
        private readonly Dictionary<ValueTuple<Type, MethodInfo>, RequestDisptachProxyClientHandler> newCompletionSourceDict = new Dictionary<ValueTuple<Type, MethodInfo>, RequestDisptachProxyClientHandler>();

        public RpcDispatchProxyFactory(ILoggerFactory loggerFactory,
                                            IServiceProvider serviceProvider,
                                            IParametersSerializer serializer,
                                            RpcMetadata metadata,
                                            TimeBasedSequence timeBasedSequence)
        {
            this.metadata = metadata;
            this.serviceProvider = serviceProvider;
            this.timeBasedSequence = timeBasedSequence;
            this.rpcClientFactory = serviceProvider.GetService<RpcClientFactory>();
            this.serializer = serializer;
            this.logger = loggerFactory.CreateLogger("F1.Core.RPC");

            this.RegisterClientProxyHandler();
        }

        //TODO: proxy缓存
        //这边可以把proxy缓存起来
        public T CreateProxy<T>(string actor = null, IActorContext context = null)
        {
            var interfaceName = typeof(T).Name;
            var o = DispatchProxy.Create<T, RpcDispatchProxy>();
            var proxy = o as RpcDispatchProxy;

            proxy.ServiceProvider = this.serviceProvider;
            proxy.DispatchProxyFactory = this;
            proxy.RpcClientFactory = this.rpcClientFactory;
            proxy.Logger = this.logger;
            proxy.Serializer = this.serializer;

            //获取实现类型, 在pd上面通过实现类型来定位
            var implType = this.metadata.GetServerType(interfaceName);
            if (implType == null) 
            {
                throw new Exception($"ActorType:{interfaceName} not found");
            }
            proxy.PositionRequest = new PlacementFindActorPositionRequest()
            {
                ActorType = interfaceName,
                ActorID = actor,
                TTL = 0,
            };
            proxy.ActorUniqueID = actor;
            proxy.Context = context;
            proxy.InterfaceType = typeof(T);
            proxy.ImplType = implType;


            if (this.logger.IsEnabled(LogLevel.Trace))
            {
                this.logger.LogTrace("CreateProxy, Type:{0}, Actor:{1}", interfaceName, actor);
            }

            return o;
        }

        private Type MakeTaskCompletionSource(Type resultType)
        {
            Type genericType = typeof(GenericCompletionSource<>);
            if (resultType != null)
                return genericType.MakeGenericType(resultType);
            else
                return genericType.MakeGenericType(typeof(object));
        }

        private Type[] GetParamsType(ParameterInfo[] ps) 
        {
            var types = new Type[ps.Length];
            for (int i = 0; i < ps.Length; ++i) 
            {
                types[i] = ps[i].ParameterType;
            }
            return types;
        }

        private Type GetTaskInnerType(Type t)
        {
            if (t.BaseType == typeof(Task) ||
                t == typeof(Task))
            {
                var resultProperty = t.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
                if (resultProperty != null)
                {
                    return resultProperty.PropertyType;
                }
            }
            return null;
        }

        internal RequestDisptachProxyClientHandler GetProxyClientHandler(Type type, MethodInfo method) 
        {
            this.newCompletionSourceDict.TryGetValue(ValueTuple.Create(type, method), out var v);
            return v;
        }

        public long GetNewSequence() 
        {
            return this.timeBasedSequence.GetNewSequence();
        }

        private void RegisterClientProxyHandler()
        {
            this.metadata.LoadAllTypes();

            foreach (var item in this.metadata.RpcClientTypes)
            {
                foreach (var method in item.Value.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var key = ValueTuple.Create(item.Value, method);
                    //不支持重载
                    if (newCompletionSourceDict.ContainsKey(key))
                    {
                        continue;
                    }

                    var isOneway = method.GetReflector().IsDefined<OnewayAttribute>();
                    var taskInnerType = this.GetTaskInnerType(method.ReturnType);
                    var paramsType = this.GetParamsType(method.GetParameters());
                    var completionSourceType = this.MakeTaskCompletionSource(taskInnerType);

                    var handler = new RequestDisptachProxyClientHandler(() =>
                    {
                        var o = (IGenericCompletionSource)Activator.CreateInstance(completionSourceType);
                        o.ID = this.GetNewSequence();
                        return o;
                    }, isOneway, $"{item.Value.Name}.{method.Name}", paramsType, taskInnerType);

                    newCompletionSourceDict.TryAdd(key, handler);

                    if (this.logger.IsEnabled(LogLevel.Trace)) 
                    {
                        this.logger.LogTrace("Register ClientProxyHandler, {0}:{1}", item.Value.Name, method.Name);
                    }
                }
            }
        }
    }
}
