using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.RPC
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class RpcAttribute : Attribute
    {
    }
}
