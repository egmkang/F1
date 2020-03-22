using F1.Core.RPC;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace F1.UnitTest.RPC
{
    public class TaskCompletionSourceManagerTest
    {
        [Fact]
        public async Task CompletionSourceTimeout()
        {
            var completionSource = new GenericCompletionSource<int>();
            completionSource.ID = 1;
            var task = completionSource.GetTask();


            var manager = new TaskCompletionSourceManager();
            manager.Push(completionSource);

            Exception E = null;
            try
            {
                await task;
            }
            catch (Exception e) 
            {
                E = e;
            }
            Assert.IsType<RpcTimeOutException>(E);
        }
    }
}
