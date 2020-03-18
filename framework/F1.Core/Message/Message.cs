using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Core.Utils;

namespace F1.Core.Message
{
    public struct InboundMessage : IInboundMessage
    {
        private object innerMessage;
        private string typeName;
        private IChannel channel;
        private long milliSecond;

        public InboundMessage(IChannel channel, string typeName, object message) 
        {
            this.channel = channel;
            this.typeName = typeName;
            this.innerMessage = message;
            this.milliSecond = Platform.GetMilliSeconds();
        }

        public string MessageName => this.typeName;

        public IChannel SourceConnection => this.channel;

        public long MilliSeconds => this.milliSecond;

        public object Inner => this.innerMessage;
    }

    public struct OutboundMessage : IOutboundMessage
    {
        private object innerMessage;
        private IChannel channel;

        public OutboundMessage(IChannel channel, object msg)
        {
            this.innerMessage = msg;
            this.channel = channel;
        }

        public IChannel DestConnection => channel;

        public object Inner => innerMessage;
    }
}
