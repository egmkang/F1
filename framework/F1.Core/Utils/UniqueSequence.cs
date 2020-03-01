using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            Contract.Assert(h * HighPartShift > Interlocked.Read(ref sequence));

            var highPart = ((long)h) * HighPartShift;
            Interlocked.Add(ref sequence, highPart);
        }

        private long sequence;
    }
}
