using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using F1.Abstractions.Placement;
using F1.Core.RPC;

namespace F1.Core.Placement
{
    public class PlacementExtension
    {
        private readonly IPlacement placement;
        private readonly RpcMetaData rpcMetadata;

        public PlacementExtension(IPlacement placement, RpcMetaData rpcMetadata) 
        {
            this.placement = placement;
            this.rpcMetadata = rpcMetadata;
        }

        static ThreadLocal<PlacementFindActorPositionRequest> pdPositionArgsCache = new ThreadLocal<PlacementFindActorPositionRequest>(() => new PlacementFindActorPositionRequest());
        public async Task<PlacementFindActorPositionResponse> FindActorPositonAsync(string actorInterfaceType, string actorID, long ttl = 0)
        {
            var args = pdPositionArgsCache.Value;
            args.ActorType = actorInterfaceType;
            args.ActorID = actorID;
            args.TTL = ttl;

            var destPosition = this.placement.FindActorPositionInCache(args);
            if (destPosition != null) return destPosition;

            args = new PlacementFindActorPositionRequest();
            args.ActorType = actorInterfaceType;
            args.ActorID = actorID;
            args.TTL = ttl;

            destPosition = await this.placement.FindActorPositonAsync(args).ConfigureAwait(false);
            return destPosition;
        }
    }
}
