using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using F1.Core.Message;


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

        public static RpcMessage AsRpcMessage(this IMessage meta) 
        {
            return new RpcMessage()
            {
                Meta = meta,
            };
        }
    }
}
