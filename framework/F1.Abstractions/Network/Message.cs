using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;

namespace F1.Abstractions.Network
{
    public struct InboundMessage
    {
        private object innerMessage;
        private string typeName;
        private IChannel channel;
        private long milliSecond;

        public InboundMessage(IChannel channel, string typeName, object message, long milliSecond) 
        {
            this.channel = channel;
            this.typeName = typeName;
            this.innerMessage = message;
            this.milliSecond = milliSecond;
        }

        public string MessageName => this.typeName;

        public IChannel SourceConnection => this.channel;

        public long MilliSeconds => this.milliSecond;

        public object Inner => this.innerMessage;


        public static readonly InboundMessage Empty = new InboundMessage(null, "", null, -1);
    }

    public struct OutboundMessage
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

        public static readonly OutboundMessage Empty = new OutboundMessage(null, null);
    }
}
