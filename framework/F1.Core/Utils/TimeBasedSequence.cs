using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.Utils
{
    public class TimeBasedSequence : UniqueSequence
    {
        public void SetTime(long time) 
        {
            this.SetHighPart(time);
        }
    }
}
