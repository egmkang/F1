using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;


namespace F1.Core.Message
{
    public class MessageCenter : IMessageCenter
    {
        public void OnConnectionClosed(IChannel channel)
        {
            throw new NotImplementedException();
        }

        public void OnMessageFail(IOutboundMessage message)
        {
            throw new NotImplementedException();
        }

        public void OnReceivedMessage(IInboundMessage message)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(IOutboundMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
