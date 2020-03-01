using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AspectCore.Extensions.Reflection;


namespace F1.Core.RPC
{
    internal class RequestDisptachProxyClientHandler
    {
        public RequestDisptachProxyClientHandler(Func<IGenericCompletionSource> f, bool isOneWay) 
        {
            this.NewCompletionSource = f;
            this.IsOneWay = isOneWay;
        }

        public Func<IGenericCompletionSource> NewCompletionSource { get; private set; }
        public bool IsOneWay { get; private set; }
    }

    public class RequestDispatchProxyFactory
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly RpcMetadata metadata;
        private readonly Dictionary<ValueTuple<Type, MethodInfo>, RequestDisptachProxyClientHandler> newCompletionSourceDict = new Dictionary<ValueTuple<Type, MethodInfo>, RequestDisptachProxyClientHandler>();

        public RequestDispatchProxyFactory(ILoggerFactory loggerFactory,
                                            IServiceProvider serviceProvider,
                                            RpcMetadata metadata)
        {
            this.metadata = metadata;
            this.serviceProvider = serviceProvider;
            this.logger = loggerFactory.CreateLogger("F1.Core.RPC");

            this.RegisterClientProxyHandler();
        }

        public T CreateProxy<T>(string actor = null, object context = null)
        {
            var o = DispatchProxy.Create<T, RequestDispatchProxy>();
            var proxy = o as RequestDispatchProxy;

            proxy.ServiceProvider = this.serviceProvider;
            proxy.DispatchProxyFactory = this;
            proxy.Logger = this.logger;

            proxy.ActorUniqueID = actor;
            proxy.Context = context;
            proxy.Type = typeof(T);

            if (this.logger.IsEnabled(LogLevel.Trace))
            {
                this.logger.LogTrace("CreateProxy, Type:{0}, Actor:{1}", proxy.Type.Name, actor);
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

        private Type GetTaskType(Type t)
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

        private void RegisterClientProxyHandler()
        {
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
                    var taskType = this.GetTaskType(method.ReturnType);
                    var completionSourceType = this.MakeTaskCompletionSource(taskType);

                    var handler = new RequestDisptachProxyClientHandler(() => (IGenericCompletionSource)Activator.CreateInstance(completionSourceType), isOneway);

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
