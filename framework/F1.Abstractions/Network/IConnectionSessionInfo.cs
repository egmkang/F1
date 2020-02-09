using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace F1.Abstractions.Network
{
    public interface IConnectionSessionInfo
    {
        long SessionID { get; }
        long ActiveTime { get; set; }
        IPEndPoint RemoteAddress { get; set; }
    }
}
