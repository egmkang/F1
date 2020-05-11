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
        const int GCInterval = 100;     //100ms做一次GC
        private readonly ConcurrentDictionary<long, IGenericCompletionSource> completionSourceDict = new ConcurrentDictionary<long, IGenericCompletionSource>();
        private readonly CompletionSourceQueue queue0 = new CompletionSourceQueue();
        private readonly CompletionSourceQueue queue1 = new CompletionSourceQueue();
        private readonly CompletionSourceQueue queue2 = new CompletionSourceQueue();
        private readonly CompletionSourceQueue queue3 = new CompletionSourceQueue();
        private readonly CompletionSourceQueue[] array = null;
        private readonly RpcTimeOutException Exception = new RpcTimeOutException();
        private readonly AtomicInt64 index = new AtomicInt64();


        public TaskCompletionSourceManager()
        {
            array = new CompletionSourceQueue[] { queue0, queue1, queue2, queue3 };

            _ = Util.RunTaskTimer(() => this.TryGCOneQueue(queue0), GCInterval);
            _ = Util.RunTaskTimer(() => this.TryGCOneQueue(queue1), GCInterval);
            _ = Util.RunTaskTimer(() => this.TryGCOneQueue(queue2), GCInterval);
            _ = Util.RunTaskTimer(() => this.TryGCOneQueue(queue3), GCInterval);
        }

        public void Push(IGenericCompletionSource completionSource)
        {
            this.completionSourceDict.TryAdd(completionSource.ID, completionSource);
            var i = index.Inc() % 4;
            var weak = new WeakCompletionSource(completionSource);
            this.array[i].Enqueue(weak);
        }

        public IGenericCompletionSource GetCompletionSource(long uniqueID)
        {
            this.completionSourceDict.TryRemove(uniqueID, out var value);
            return value;
        }

        private const int TimeOut = 5 * 1000;
        private void TryGCOneQueue(CompletionSourceQueue q) 
        {
            var currentTime = Platform.GetMilliSeconds() - TimeOut;

            while (q.TryPeek(out var item)) 
            {
                if (item.TryGetTarget(out var completionSource) &&
                    !completionSource.GetTask().IsCompleted)
                {
                    if (completionSource.CreateTime > currentTime)
                    {
                        break;
                    }
                    else 
                    {
                        this.GetCompletionSource(completionSource.ID);
                        completionSource.WithException(Exception);
                    }
                }
                q.TryDequeue(out var _);
            }
        }
    }
}
