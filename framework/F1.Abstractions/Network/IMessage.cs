using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;

namespace F1.Abstractions.Network
{
    public interface IInboundMessage
    {
        string MessageName { get; }
        IChannel SourceConnection { get; }
        long MilliSeconds { get; }
        object Inner { get; }
    }

    public interface IOutboundMessage 
    {
        IChannel DestConnection { get; }
        object Inner { get; }
    }
}
