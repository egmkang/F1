using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using F1.Abstractions.RPC;

namespace Sample.Interface
{
    [Rpc]
    public interface IPlayer
    {
        Task SetAccount(string account);
        Task<string> EchoAsync(string name);
    }
}
