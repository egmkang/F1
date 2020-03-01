using F1.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace F1.Core.RPC
{
    internal interface IGenericCompletionSource
    {
        void WithResult(object o);

        void WithException(Exception e);

        Task GetTask();

        long ID { get; set; }
    }

    internal class GenericCompletionSource<T> : TaskCompletionSource<T>, IGenericCompletionSource
    {
        public Task GetTask() { return this.Task;  }

        public void WithException(Exception e)
        {
            this.SetException(e);
        }

        public void WithResult(object o)
        {
            this.SetResult((T)o);
        }

        public long ID { get; set; }
    }
}
