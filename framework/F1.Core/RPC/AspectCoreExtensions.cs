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
    public interface IAspectCoreExtensions 
    {
        Type GetClientType(string t);

        Type GetServerType(string t);

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

    public class AspectExtensionException : Exception 
    {
        public AspectExtensionException(string message) : base(message) { }
    }

    public delegate AsyncReturnValue ServerInvoker(object instance, object[] param);

    public delegate object GetTaskResult(Task t);

    public class AspectCoreExtensions : IAspectCoreExtensions
    {
        //interface => implementation
        private readonly Dictionary<Type, Type> rpcTypesDict = new Dictionary<Type, Type>();
        private readonly Dictionary<string, Type> rpcClientTypes = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> rpcServerTypes = new Dictionary<string, Type>();


        private readonly Dictionary<string, ServerInvoker> invokersMap = new Dictionary<string, ServerInvoker>();
        private readonly Dictionary<string, GetTaskResult> returnValuesMap = new Dictionary<string, GetTaskResult>();

        private readonly ILogger logger;

        public AspectCoreExtensions(ILoggerFactory loggerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.RPC");

            this.Init();
        }


        private void Init() 
        {
            this.GetAllRpcTypes();
            this.RegisterRpcTypes();
            this.CreateRpcFunc();
        }

        private void CreateRpcFunc() 
        {
            foreach (var item in rpcClientTypes)
            {
                var implType = this.GetServerType(item.Key);
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

                    var returnType = method.ReturnType;
                    if (returnType.BaseType == typeof(Task) ||
                        returnType == typeof(Task))
                    {
                        var resultProperty = returnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
                        if (resultProperty != null)
                        {
                            var reflector = resultProperty.GetReflector();
                            returnValuesMap.Add(uniqueName, (o) =>
                            {
                                return reflector.GetValue(o);
                            });
                        }
                    }

                    returnValuesMap.TryGetValue(uniqueName, out var taskProperty); 

                    invokersMap.Add(uniqueName, (instance, param) => 
                    {
                        var ret = refector.Invoke(instance, param);
                        return new AsyncReturnValue(ret, taskProperty);
                    });

                    this.logger.LogTrace("Register ServerHandler, {0}", uniqueName);
                }
            }
        }

        private void RegisterRpcTypes()
        {
            foreach (var (clientType, serverType) in rpcTypesDict)
            {
                var name = clientType.Name;
                rpcClientTypes.TryAdd(name, clientType);
                rpcServerTypes.TryAdd(name, serverType);

                if (serverType != null)
                {
                    this.logger.LogTrace("Register RPC Type:{0}", name);
                }
            }
        }

        public Type GetClientType(string t)
        {
            rpcClientTypes.TryGetValue(t, out var tt);
            return tt;
        }

        public Type GetServerType(string t) 
        {
            rpcServerTypes.TryGetValue(t, out var tt);
            return tt;
        }

        private Dictionary<Type, Type> GetAllRpcTypes()
        {
            if (this.rpcTypesDict.Count != 0) 
            {
                return this.rpcTypesDict;
            }

            var alltTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(m =>
            {
                try { return m.GetExportedTypes(); }
                catch { return new Type[0]; }
            }).ToList();

            var rpcTypes = new List<ValueTuple<Type, Type>>();

            foreach (var type in alltTypes)
            {
                try
                {
                    var reflector = type.GetReflector();
                    var implTypes = type.GetInterfaces()
                        .Where(j => j.GetReflector().IsDefined<RpcAttribute>())
                        .Select(j => new ValueTuple<Type, Type>(j, type));

                    if (type.IsInterface && reflector.IsDefined<RpcAttribute>())
                    {
                        rpcTypes.Add(new ValueTuple<Type, Type>(type, null));
                    }
                    else
                    {
                        rpcTypes.AddRange(implTypes);
                    }
                }
                catch (Exception e) { }
            }

            foreach (var (define, impl) in rpcTypes) 
            {
                if (this.rpcTypesDict.TryGetValue(define, out var value))
                {
                    if (impl != null)
                    {
                        this.rpcTypesDict[define] = impl;
                    }
                }
                else 
                {
                    this.rpcTypesDict.TryAdd(define, impl);
                }
            }
            return this.rpcTypesDict;
        }

        public AsyncReturnValue Invoke(string name, object instance, object[] param)
        {
            this.invokersMap.TryGetValue(name , out var func);
            if (func != null) 
            {
                return func(instance, param);
            }
            throw new AspectExtensionException("Method Not Found");
        }
    }
}
