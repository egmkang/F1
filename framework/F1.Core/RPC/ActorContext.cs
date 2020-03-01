using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.RPC
{
    public class ActorContext
    {
        public long ServerID { get; set; }
        public long SourceID { get; set; }


        public void SendMessage(object o) { }
    }
}
