using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions.Network
{
    public interface IConnectionManager
    {
        void AddConnection(IChannel channel);

        void RemoveConnection(IChannel channel);

        IChannel GetConnection(long sessionID);
    }
}
