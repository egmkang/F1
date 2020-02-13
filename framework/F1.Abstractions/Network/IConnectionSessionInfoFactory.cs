using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;

namespace F1.Abstractions.Network
{
    public interface IConnectionSessionInfoFactory
    {
        IConnectionSessionInfo NewSessionInfo(IMessageHandlerFactory factory);
    }
}
