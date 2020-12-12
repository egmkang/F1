using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading;

namespace F1.Core.Utils
{
    public class UniqueSequence
    {
        public const long HighPartShift = 10_000_000_000;

        public long GetNewSequence() 
        {
            return Interlocked.Increment(ref sequence);
        }

        public void SetHighPart(long h)
        {
            Contract.Assert(h * HighPartShift > Interlocked.Read(ref sequence));

            var highPart = ((long)h) * HighPartShift;
            Interlocked.Add(ref sequence, highPart);
        }

        private long sequence;
    }
}
