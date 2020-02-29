using System;

namespace F1.Core.RPC
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class OnewayAttribute : Attribute
    {
    }
}
