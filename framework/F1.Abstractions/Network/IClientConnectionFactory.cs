using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace F1.Abstractions.Network
{
    public interface IClientConnectionFactory
    {
        void Init();
        Task<IChannel> ConnectAsync(EndPoint address, IMessageHandlerFactory factory);
        Task ShutdDownAsync();
    }
}
