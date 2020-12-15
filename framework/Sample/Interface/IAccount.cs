using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using F1.Abstractions.RPC;

namespace F1.Sample.Interface
{
    [Rpc]
    public interface IAccount
    {
        Task AuthTokenAsync(byte[] token);
    }
}
