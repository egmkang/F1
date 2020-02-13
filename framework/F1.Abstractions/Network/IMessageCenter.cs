using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;

namespace F1.Abstractions.Network
{
    public interface IMessageCenter
    {
        void RegsiterEvent(Action<IInboundMessage> inboundMessageProc,
            Action<IChannel> channelClosedProc,
            Action<IOutboundMessage> failMessageProc);
        void SendMessage(IOutboundMessage message);
        void OnReceivedMessage(IInboundMessage message);
        void OnMessageFail(IOutboundMessage message);
        void OnConnectionClosed(IChannel channel);
    }
}
