using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.Config
{
    public class GatewayConfiguration
    {
        public string PlacementDriverAddress { get; set; }
        public int ListenPort { get; set; }
        public int GatewayPort { get; set; }
    }
}
