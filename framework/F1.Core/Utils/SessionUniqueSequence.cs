using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.Utils
{
    public class SessionUniqueSequence: UniqueSequence
    {
        public static long GetServerID(long seq) 
        {
            return seq / HighPartShift;
        }

        public void SetServerID(long serverID) 
        {
            this.SetHighPart(serverID);
        }

        public long NewSessionID => this.GetNewSequence();
    }
}
