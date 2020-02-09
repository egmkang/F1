using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using F1.Abstractions.Network;
using F1.Core.Utils;

namespace F1.Core.Message
{
    public class InboundMessage : IInboundMessage
    {
        private IMessage innerMessage;
        private IChannel channel;
        private long milliSecond = Platform.GetMilliSeconds();

        public InboundMessage(IChannel channel, IMessage message) 
        {
            this.channel = channel;
            this.innerMessage = message;
        }

        public string MessageName => this.innerMessage?.Descriptor.FullName;

        public IChannel SourceConnection => this.channel;

        public long MilliSeconds => this.milliSecond;

        public object Inner => this.innerMessage;
    }

    public class OutboundMessage : IOutboundMessage
    {
        private IMessage innerMessage;
        private IChannel channel;

        public OutboundMessage(IChannel channel, IMessage msg) 
        {
            this.innerMessage = msg;
            this.channel = channel;
        }

        public IChannel DestConnection => channel;

        public object Inner => innerMessage;
    }
}
