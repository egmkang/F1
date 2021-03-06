using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using F1.Core.Message;
using F1.Abstractions.Network;

namespace F1.Core.Utils
{
    public static class Extensions
    {
        public static void Resize<T>(this List<T> list, int sz, T c)
        {
            int cur = list.Count;
            if (sz < cur)
                list.RemoveRange(sz, cur - sz);
            else if (sz > cur)
            {
                if (sz > list.Capacity)
                    list.Capacity = sz;
                list.AddRange(Enumerable.Repeat(c, sz - cur));
            }
        }
        public static void Resize<T>(this List<T> list, int sz)
        {
            Resize(list, sz, default(T));
        }

        public static RpcMessage ToRpcMessage(this IMessage meta, byte[] body=null) 
        {
            var m = new RpcMessage() { Meta = meta, };
            if (body != null) 
            {
                m.Body = body;
            }
            return m;
        }

        public static (IMessage meta, byte[] body) GetRpcMessage(this InboundMessage inboundMessage)
        {
            if (inboundMessage.Inner is RpcMessage rpcMessage)
            {
                return (rpcMessage.Meta, rpcMessage.Body);
            }
            return (null, null);
        }
    }
}
