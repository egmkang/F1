using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions.RPC
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class RpcAttribute : Attribute
    {
    }
}
