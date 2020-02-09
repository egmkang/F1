using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using F1.Abstractions.Network;

namespace F1.Core.Network
{
    public class DefaultChannelSessionInfo : IConnectionSessionInfo
    {
        private readonly long sessionID;
        private long activeTime;
        private IPEndPoint address;

        public DefaultChannelSessionInfo(long sessionID) 
        {
            this.sessionID = sessionID;
        }
        public long SessionID => sessionID;

        public long ActiveTime { get => activeTime; set => activeTime = value; }
        public IPEndPoint RemoteAddress { get => address; set => address = value; }
    }
}
