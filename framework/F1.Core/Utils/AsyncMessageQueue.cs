using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
        private int queueCount = 0;

        public AsyncMessageQueue(ILogger logger) 
        {
            this.Valid = true;
            this.logger = logger;
        }

        public bool PushMessage(T message) 
        {
            Interlocked.Increment(ref this.queueCount);
            if (!this.channel.Writer.TryWrite(message)) 
            {
                Interlocked.Decrement(ref this.queueCount);
                return false;
            }
            return true;
        }

        public void ShutDown() 
        {
            this.Valid = false;
            this.channel.Writer.TryWrite(default(T));
            this.channel.Writer.Complete();
        }

        public bool Valid { get; private set; }
        public int QueueCount => Interlocked.Add(ref this.queueCount, 0);
        public void DecQueueCount() 
        {
            Interlocked.Decrement(ref this.queueCount);
        }
        public ChannelReader<T> Reader => this.channel.Reader;
    }
}
