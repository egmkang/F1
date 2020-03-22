using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;

namespace F1.Abstractions.Network
{
    public interface IMessageCenter
    {
        void RegsiterEvent(Action<IChannel> channelClosedProc,
            Action<OutboundMessage> failMessageProc);
        /// <summary>
        /// 注册消息的回调函数, messageName为""时是默认的回调函数
        /// </summary>
        /// <param name="messageName">消息的名字</param>
        /// <param name="inboundMessage">需要被处理的消息</param>
        void RegisterMessageProc(string messageName, Action<InboundMessage> inboundMessageProc);
        void SendMessage(OutboundMessage message);
        void OnReceivedMessage(InboundMessage message);
        void OnMessageFail(OutboundMessage message);
        void OnConnectionClosed(IChannel channel);
    }
}
