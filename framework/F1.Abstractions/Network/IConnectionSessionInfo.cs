﻿using System;
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
        long ActiveTime { get; set; }
        IPEndPoint RemoteAddress { get; set; }

        int PutOutboundMessage(IOutboundMessage msg);
        int SendingQueueCount { get; }
        void RunSendLoopAsync(IChannel channel);
        void ShutDown();
    }
}