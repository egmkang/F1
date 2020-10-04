using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.RPC;

namespace F1.Abstractions.Gateway
{
    /// <summary>
    /// Gateway只有一个空的接口, 用来做服务发现
    /// Gateway和ActorHost通过消息通讯, 不是通过RPC来通讯
    /// </summary>
    [Rpc]
    public interface IGateway
    {
    }
}
