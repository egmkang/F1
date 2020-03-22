using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Channels;
using DotNetty.Transport.Channels;

namespace F1.Abstractions.Network
{
    public interface IConnectionSessionInfo
    {
        long SessionID { get; }
        long ServerID { get; set; }
        long ActiveTime { get; set; }
        IPEndPoint RemoteAddress { get; set; }
        bool IsActive { get; }

        int PutOutboundMessage(OutboundMessage msg);
        void RunSendingLoopAsync(IChannel channel);
        void ShutDown();
    }
}
