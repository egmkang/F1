using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Codecs;

namespace F1.Abstractions.Network
{
    public interface IMessageHandlerFactory
    {
        ByteToMessageDecoder NewHandler();
    }
}
