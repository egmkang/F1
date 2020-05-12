using DotNetty.Transport.Channels;
using F1.Core.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;

namespace F1.Core.Network
{

    internal class SendingMessageThread
    {
        private readonly object mutex = new object();
        private Dictionary<long, IChannel> channels = new Dictionary<long, IChannel>();
        private Dictionary<long, IChannel> temp = new Dictionary<long, IChannel>();
        private readonly Thread thread;
        private volatile int stop = 0;
        private readonly AtomicInt64 pendingCount = new AtomicInt64();

        public SendingMessageThread() 
        {
            this.thread = new Thread(this.SendingLoop);
            this.thread.Start();
        }

        public void SendMessage(IChannel channel) 
        {
            var sessionInfo = channel.GetSessionInfo();
            lock (this.mutex) 
            {
                channels.TryAdd(sessionInfo.SessionID, channel);
                pendingCount.Inc();
            }
        }

        const int MaxSpinCount = 128;
        public void SendingLoop() 
        {
            var emptyCount = 0;
            while (stop == 0) 
            {
                var count = 0L;
                lock (this.mutex) 
                {
                    var t = this.channels;
                    channels = temp;
                    temp = t;
                    this.channels.Clear();
                    count = this.pendingCount.Load();
                    if (count != 0)
                    {
                        this.pendingCount.Add(-count);
                    }
                }

                if (count == 0)
                {
                    emptyCount++;
                    if (emptyCount <= 128) continue;
                    Thread.Sleep(10);
                }
                emptyCount = 0;

                foreach (var (_, channel) in temp) 
                {
                    var sessionInfo = channel.GetSessionInfo();
                    sessionInfo.SendMessagesBatch(channel);
                }
            }
        }
    }

    public class SendingThreads
    {
        private readonly SendingMessageThread[] array = new SendingMessageThread[2] { new SendingMessageThread(), new SendingMessageThread() };

        public void SendMessage(IChannel channel) 
        {
            if (channel == null) return;
            var sessionInfo = channel.GetSessionInfo();
            if (sessionInfo == null) return;

            array[sessionInfo.SessionID % 2].SendMessage(channel);
        }
    }
}
