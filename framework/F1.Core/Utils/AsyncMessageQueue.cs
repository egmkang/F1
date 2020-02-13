using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;


namespace F1.Core.Utils
{
    internal class AsyncMessageQueue<T> where T : class
    {
        private readonly Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        private readonly ILogger logger;

        public AsyncMessageQueue(ILogger logger) 
        {
            this.Valid = true;
            this.logger = logger;
        }

        public void PushMessage(T message) 
        {
            if (!this.channel.Writer.TryWrite(message)) 
            {
                this.logger.LogError("Dropping Message, because queue is closed");
            }
        }

        public void ShutDown() 
        {
            this.Valid = false;
            this.channel.Writer.TryWrite(default(T));
            this.channel.Writer.Complete();
        }

        public bool Valid { get; private set; }
        public ChannelReader<T> Reader => this.channel.Reader;
    }
}
