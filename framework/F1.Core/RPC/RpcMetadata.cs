using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AspectCore.Extensions.Reflection;
using Microsoft.Extensions.Logging;
using F1.Abstractions.RPC;

namespace F1.Core.RPC
{
    public class RpcMetadata
    {
        private readonly ILogger logger;
        //interface => implementation
        private readonly Dictionary<Type, Type> rpcTypesDict = new Dictionary<Type, Type>();
        private readonly Dictionary<string, Type> rpcClientTypes = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> rpcServerTypes = new Dictionary<string, Type>();

        public RpcMetadata(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger("F1.Core.RPC");
        }

        public Dictionary<Type, Type> RpcTypesDict => RpcTypesDict;

        public Dictionary<string, Type> RpcClientTypes => rpcClientTypes;

        public Dictionary<string, Type> RpcServerTypes => rpcServerTypes;

        public Type GetServerType(string name) 
        {
            this.rpcServerTypes.TryGetValue(name, out var value);
            return value;
        }

        public void LoadAllTypes()
        {
            if (this.rpcTypesDict.Count != 0)
            {
                return;
            }
            this.LoadRpcTypes();
            this.RegisterRpcTypes();
        }

        private void LoadRpcTypes()
        {
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
        }

        private void RegisterRpcTypes ()
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
    }
}
