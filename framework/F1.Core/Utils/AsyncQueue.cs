using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace F1.Core.Utils
{
    internal class AsyncQueue<T> 
    {
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);

        public void Enqueue(T t) 
        {
            this.queue.Enqueue(t);
            this.semaphore.Release();
        }

        public async ValueTask<ConcurrentQueue<T>> ReadAsync()
        {
            await this.semaphore.WaitAsync().ConfigureAwait(false);
            return queue;
        }
    }
}
