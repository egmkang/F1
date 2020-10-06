using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using F1.Abstractions.RPC;

namespace F1.Sample.Interface
{
    [Rpc]
    public interface IPlayer
    {
        Task AuthTokenAsync(byte[] token);
        Task<string> SayHelloAsync(string name);
    }
}
