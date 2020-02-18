using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.Placement;

namespace F1.Core.Placement
{
    public class PlacementServerList
    {
        private Dictionary<long, PlacementActorHostInfo> servers = new Dictionary<long, PlacementActorHostInfo>();
    }
}
