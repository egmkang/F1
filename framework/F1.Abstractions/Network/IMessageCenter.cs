using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;

namespace F1.Abstractions.Network
{
    public interface IMessageCenter
    {
        void RegsiterEvent(Action<IChannel> channelClosedProc,
            Action<IOutboundMessage> failMessageProc);
        /// <summary>
        /// 注册消息的回调函数, messageName为""时是默认的回调函数
        /// </summary>
        /// <param name="messageName">消息的名字</param>
        /// <param name="inboundMessage">需要被处理的消息</param>
        void RegisterMessageProc(string messageName, Action<IInboundMessage> inboundMessageProc);
        void SendMessage(IOutboundMessage message);
        void OnReceivedMessage(IInboundMessage message);
        void OnMessageFail(IOutboundMessage message);
        void OnConnectionClosed(IChannel channel);
    }
}
