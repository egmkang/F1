using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace F1.Core.Utils
{
    public class UniqueSequence
    {
        const long HighPartShift = 10000000000;

        public long GetNewSequence() 
        {
            return Interlocked.Increment(ref sequence);
        }

        public void SetHighPart(int h)
        {
            var highPart = ((long)h) * HighPartShift;
            var s = Interlocked.Read(ref sequence);
            Interlocked.Add(ref sequence, highPart - s);
        }

        private long sequence;
    }
}
