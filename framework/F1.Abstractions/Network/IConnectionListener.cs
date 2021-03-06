using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace F1.Abstractions.Network
{
    public interface IConnectionListener
    {
        void Init();
        Task BindAsync(int port, IMessageHandlerFactory factory);
        Task ShutdDownAsync();
    }
}
