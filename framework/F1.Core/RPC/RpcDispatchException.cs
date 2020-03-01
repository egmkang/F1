using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.RPC
{
    public class RpcDispatchException : Exception 
    {
        public RpcDispatchException(string message) : base(message) { }
    }
}
