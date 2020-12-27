using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.RPC
{
    public class RpcNewPositionException : Exception 
    {
    }

    public class RpcDispatchException : Exception 
    {
        public RpcDispatchException(string message) : base(message) { }
    }

    public class RpcTimeOutException : Exception
    {
        public RpcTimeOutException() : base("timeout") { }
    }
}
