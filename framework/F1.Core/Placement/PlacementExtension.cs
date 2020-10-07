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
        private readonly RpcMetadata rpcMetadata;

        public PlacementExtension(IPlacement placement, RpcMetadata rpcMetadata) 
        {
            this.placement = placement;
            this.rpcMetadata = rpcMetadata;
        }

        static ThreadLocal<PlacementFindActorPositionRequest> pdPositionArgsCache = new ThreadLocal<PlacementFindActorPositionRequest>(() => new PlacementFindActorPositionRequest());
        public async Task<PlacementFindActorPositionResponse> FindActorPositonAsync(string actorInterfaceType, string actorID, long ttl = 0)
        {
            //TODO:
            //等PD修改好了之后, 就不需要查询本地的实现类型
            var implType = this.rpcMetadata.GetServerType(actorInterfaceType);
            if (implType == null)
            {
                throw new Exception($"ActorInterfaceType:{actorInterfaceType} not found ImplType");
            }
            var args = pdPositionArgsCache.Value;
            args.ActorInterfaceType = actorInterfaceType;
            args.ActorImplType = implType.Name;
            args.ActorID = actorID;
            args.TTL = ttl;

            var destPosition = this.placement.FindActorPositionInCache(args);
            if (destPosition != null) return destPosition;

            args = new PlacementFindActorPositionRequest();
            args.ActorInterfaceType = actorInterfaceType;
            args.ActorImplType = implType.Name;
            args.ActorID = actorID;
            args.TTL = ttl;

            destPosition = await this.placement.FindActorPositonAsync(args).ConfigureAwait(false);
            return destPosition;
        }
    }
}
