using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AspectCore.Extensions.Reflection;

namespace F1.Core.RPC
{
    public interface IRpcRequestDispatchHandler 
    {
        /// <summary>
        /// 在服务端侧调用具体的实现, 通过name来定位到handler, 然后执行获取返回值
        /// 返回值可以是Task和Task<T>
        /// </summary>
        /// <param name="name">接口类型的名字</param>
        /// <param name="instance">接口的实例</param>
        /// <param name="param">传入参数</param>
        /// <returns>返回值</returns>
        AsyncReturnValue Invoke(string name, object instance, object[] param);
    }

    public struct AsyncReturnValue 
    {
        private object returnValue;
        private GetTaskResult func;

        public AsyncReturnValue(object r, GetTaskResult func) 
        {
            this.returnValue = r;
            this.func = func;
        }

        public async Task<object> GetReturnValueAsync() 
        {
            var t = returnValue as Task;
            if (t == null) 
            {
                return returnValue; 
            }

            if (!t.IsCompleted)
            {
                await t;
            }

            return this.func != null ? this.func(t) : null;
        }
    }

    public delegate AsyncReturnValue ServerInvoker(object instance, object[] param);

    public delegate object GetTaskResult(Task t);

    public class RequestDispatchHandler : IRpcRequestDispatchHandler
    {
        private readonly RpcMetadata metadata;
        private readonly Dictionary<string, ServerInvoker> invokersMap = new Dictionary<string, ServerInvoker>();
        private readonly Dictionary<string, (Type[] InputArgsType, GetTaskResult GetReturnValueAction)> argsMap = new Dictionary<string, (Type[], GetTaskResult)>();

        private readonly ILogger logger;

        public RequestDispatchHandler(ILoggerFactory loggerFactory, RpcMetadata metadata) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.RPC");
            this.metadata = metadata;

            this.Init();
        }


        private void Init() 
        {
            this.metadata.LoadAllTypes();

            this.CreateRpcFunc();
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

        private void CreateRpcFunc() 
        {
            foreach (var item in this.metadata.RpcClientTypes)
            {
                var implType = this.metadata.GetServerType(item.Key);
                if (implType == null) continue;

                foreach (var method in item.Value.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var uniqueName = $"{item.Key}.{method.Name}";
                    //不支持重载
                    if (invokersMap.ContainsKey(uniqueName)) 
                    {
                        continue;
                    }
                    var refector = method.GetReflector();

                    var paramsType = this.GetParamsType(method.GetParameters());
                    var getReturnValue = null as GetTaskResult;

                    var returnType = method.ReturnType;
                    if (returnType.BaseType == typeof(Task) ||
                        returnType == typeof(Task))
                    {
                        var resultProperty = returnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
                        if (resultProperty != null)
                        {
                            var reflector = resultProperty.GetReflector();
                            getReturnValue = (o) => reflector.GetValue(o);
                        }
                    }

                    argsMap.TryAdd(uniqueName, (paramsType, getReturnValue));
                    argsMap.TryGetValue(uniqueName, out var argsInfo); 

                    invokersMap.Add(uniqueName, (instance, param) => 
                    {
                        var ret = refector.Invoke(instance, param);
                        return new AsyncReturnValue(ret, argsInfo.GetReturnValueAction);
                    });

                    this.logger.LogTrace("Register ServerHandler, {0}", uniqueName);
                }
            }
        }

        public Type[] GetInputArgsType(string name) 
        {
            this.argsMap.TryGetValue(name, out var argsInfo);
            return argsInfo.InputArgsType;
        }

        public AsyncReturnValue Invoke(string name, object instance, object[] param)
        {
            this.invokersMap.TryGetValue(name, out var func);
            if (func != null) 
            {
                return func(instance, param);
            }
            throw new RpcDispatchException("Method Not Found");
        }
    }
}
