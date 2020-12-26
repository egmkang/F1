using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using F1.Core.Message;


namespace F1.UnitTest
{

    public class LoadAssembly : IDisposable
    {
        static object mutex = new object();
        static volatile int v = 0;

        public LoadAssembly() 
        {
            if (v != 0) return;
            lock (mutex) 
            {
                if (v != 0) return;
                MessageExt.Load();
                v = 1;
            }
        }

        public void Dispose()
        {
        }
    }

    public class Setup : IClassFixture<LoadAssembly>
    {
    }
}
