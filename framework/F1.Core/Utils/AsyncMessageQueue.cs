using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;


namespace F1.Core.Utils
{
    internal class AsyncMessageQueue<T>
    {
        private readonly Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        private int queueCount = 0;

        public AsyncMessageQueue() 
        {
            this.Valid = true;
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

        public void ShutDown(T empty) 
        {
            this.Valid = false;
            this.channel.Writer.TryWrite(empty);
            this.channel.Writer.Complete();
        }

        public bool Valid { get; private set; }
        public int QueueCount
        {
            get => Interlocked.Add(ref this.queueCount, 0);
            set => Interlocked.Decrement(ref this.queueCount);
        }
        public ChannelReader<T> Reader => this.channel.Reader;
    }
}
