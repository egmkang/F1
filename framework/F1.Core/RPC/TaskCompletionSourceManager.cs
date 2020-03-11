using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using F1.Core.Utils;

namespace F1.Core.RPC
{
    using WeakCompletionSource = WeakReference<IGenericCompletionSource>;
    using CompletionSourceQueue = ConcurrentQueue<WeakReference<IGenericCompletionSource>>;

    public class TaskCompletionSourceManager
    {
        class QueueGroup
        {
            private readonly CompletionSourceQueue queue0 = new CompletionSourceQueue();
            private readonly CompletionSourceQueue queue1 = new CompletionSourceQueue();
            private readonly CompletionSourceQueue queue2 = new CompletionSourceQueue();
            private readonly CompletionSourceQueue queue3 = new CompletionSourceQueue();
            private readonly RpcTimeOutException Exception = new RpcTimeOutException();
            private readonly AtomicInt64 next = new AtomicInt64();
            private readonly AtomicInt64 count = new AtomicInt64();

            public long Count => count;

            public void Push(IGenericCompletionSource completionSource)
            {
                this.count.Inc();
                var index = this.next.Inc() % 4;
                var s = new WeakCompletionSource(completionSource);
                switch (index)
                {
                    case 0: queue0.Enqueue(s); break;
                    case 1: queue1.Enqueue(s); break;
                    case 2: queue2.Enqueue(s); break;
                    case 3: queue3.Enqueue(s); break;
                }
            }

            public void GC(Action<long> action)
            {
                foreach (var q in new CompletionSourceQueue[] { queue0, queue1, queue2, queue3 })
                {
                    foreach (var item in q)
                    {
                        var uniqueID = 0L;
                        try
                        {
                            if (item.TryGetTarget(out var value) && value != null && !value.GetTask().IsCompleted)
                            {
                                uniqueID = value.ID;
                                value.WithException(Exception);
                            }
                        }
                        finally
                        {
                            if (uniqueID != 0)
                            {
                                action(uniqueID);
                            }
                        }
                    }
                }
            }
        }

        const int GCDelay = 2100;       //2.1秒之后
        const int GCInterval = 100;     //100ms做一次GC
        private readonly ConcurrentDictionary<long, IGenericCompletionSource> completionSourceDict = new ConcurrentDictionary<long, IGenericCompletionSource>();
        private readonly ConcurrentQueue<QueueGroup> queue = new ConcurrentQueue<QueueGroup>();
        private QueueGroup currentGroup = new QueueGroup();


        public TaskCompletionSourceManager()
        {
            this.GCNextQueueLoop();
            this.ChangeNextQueueLoop();
        }

        public void Push(IGenericCompletionSource completionSource)
        {
            this.completionSourceDict.TryAdd(completionSource.ID, completionSource);
            this.currentGroup.Push(completionSource);
        }

        public IGenericCompletionSource GetCompletionSource(long uniqueID)
        {
            this.completionSourceDict.TryRemove(uniqueID, out var value);
            return value;
        }

        private async void GCNextQueueLoop()
        {
            await Task.Delay(GCDelay);

            await Util.RunTaskTimer(() =>
            {
                this.queue.TryDequeue(out var q);
                if (q != null) { q.GC((id) => this.GetCompletionSource(id)); }
            }, GCInterval);
        }

        private async void ChangeNextQueueLoop()
        {
            await Util.RunTaskTimer(() =>
            {
                if (this.currentGroup.Count == 0) return;

                this.queue.Enqueue(this.currentGroup);
                var q = new QueueGroup();
                this.currentGroup = q;
            }, GCInterval);
        }
    }
}
