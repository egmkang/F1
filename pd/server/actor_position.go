package server

import (
	"fmt"
	"github.com/pingcap/log"
	"github.com/pkg/errors"
	"go.etcd.io/etcd/clientv3"
	"go.uber.org/zap"
	"math/rand"
	"pd/server/util"
)

const AliveServerTime = 15 * 1000 //起码要存活超过15秒以上, 才会被系统分配到Actor
const ActorPathPrefix = "/actor"

type ActorPositionInfo struct {
	ActorID    string `json:"actor_id"`
	ActorType  string `json:"actor_type"`
	Domain     string `json:"domain"`
	TTL        int64  `json:"ttl"`
	CreateTime int64  `json:"create_time"`
	ServerID   int64  `json:"server_id"`
}

type ActorPositionArgs struct {
	ActorID   string
	ActorType string
	Domain    string
	TTL       int64
}

func (this *ActorMembership) findPositionInLRU(uniqueActorID string, needAllocNewPosition bool) *ActorPositionInfo {
	cache, ok := this.actorPositionCache.Get(uniqueActorID)
	if !ok {
		return nil
	}
	position, _ := cache.(*ActorPositionInfo)
	if needAllocNewPosition {
		if this.checkServerAlive(position.ServerID) {
			return position
		}
		return nil
	}
	return position
}

func (this *ActorMembership) checkServerAlive(serverID int64) bool {
	info := this.GetActorMemberInfoByID(serverID)
	return info != nil
}

func (this *ActorMembership) savePositionToLRU(uniqueActorID string, position *ActorPositionInfo) {
	this.actorPositionCache.Add(uniqueActorID, position)
}

func tryParseActorPositionInfo(data []byte) *ActorPositionInfo {
	info := &ActorPositionInfo{}
	err := util.ReadJSONFromData(data, info)
	if err != nil {
		log.Error("tryParseActorPositionInfo fail", zap.Error(err))
		return nil
	}
	return info
}

func (this *ActorMembership) findPositionFromRemote(uniqueActorID string, needAllocNewPosition bool) (*ActorPositionInfo, error) {
	key := fmt.Sprintf("%s/%s", ActorPathPrefix, uniqueActorID)
	data, err := util.EtcdGetKVValue(this.server.GetEtcdClient(), key)
	if err != nil {
		log.Error("findPositionFromRemote", zap.Error(err))
		return nil, err
	}
	if data == nil {
		return nil, nil
	}

	info := tryParseActorPositionInfo(data)
	if needAllocNewPosition {
		if this.checkServerAlive(info.ServerID) {
			return info, nil
		}
		return nil, nil
	}
	return info, nil
}

func (this *ActorMembership) removePositionFromRemote(uniqueActorID string) error {
	key := fmt.Sprintf("%s/%s", ActorPathPrefix, uniqueActorID)
	_, err := util.EtcdKVDelete(this.server.GetEtcdClient(), key)
	return err
}

func (this *ActorMembership) savePositionToRemote(uniqueActorID string, position *ActorPositionInfo) error {
	key := fmt.Sprintf("%s/%s", ActorPathPrefix, uniqueActorID)
	json, err := util.JSON(position)
	if err != nil {
		log.Error("savePositionToRemote", zap.Error(err))
		return err
	}
	var leaseID = int64(0)
	if position.TTL > 0 {
		resp, err := util.EtcdLeaseGrant(this.server.GetEtcdClient(), position.TTL)
		if err != nil {
			log.Error("savePositionToRemote", zap.Error(err))
			return err
		}
		leaseID = int64(resp.ID)
	}
	if leaseID != 0 {
		_, err := util.EtcdKVPut(this.server.GetEtcdClient(), key, json, clientv3.WithLease(clientv3.LeaseID(leaseID)))
		if err != nil {
			log.Error("savePositionToRemote", zap.Error(err))
			return err
		}
	} else {
		_, err := util.EtcdKVPut(this.server.GetEtcdClient(), key, json)
		if err != nil {
			log.Error("savePositionToRemote", zap.Error(err))
			return err
		}
	}
	return nil
}

//-1服务器个数不够
//-2没有该类型的服务器
//-3分配算法错误
func (this *ActorMembership) chooseServerByRandom(domain string, actorType string) int64 {
	//返回-1表示服务器个数不够
	index := this.index
	uniqueType := fmt.Sprintf("%s:%s", domain, actorType)
	set, ok := index.types[uniqueType]
	if !ok || len(set) == 0 {
		return -2
	}

	now := util.GetMilliSeconds()

	max := int64(10)
	loads := make([]int64, 0)
	servers := make([]*ActorHostInfo, 0)
	for _, v := range set {
		//需要加入到集群一段时间, 然后当前负载是正数
		if now-v.StartTime >= AliveServerTime && v.Load >= 0 {
			servers = append(servers, v)
			loads = append(loads, v.Load)
			if v.Load > max {
				max = v.Load
			}
		}
	}

	if len(servers) == 0 {
		return -1
	}

	max = max * 11 / 10
	sum := int64(0)
	for index, v := range loads {
		loads[index] = max - v
		sum += max - v
	}

	random := rand.Int63n(max)

	for index, v := range loads {
		random -= v
		if random <= 0 {
			return servers[index].ServerID
		}
	}
	return -3
}

func (this *ActorMembership) findOrAllocNewPosition(args *ActorPositionArgs) (*ActorPositionInfo, error) {
	needAllocNewPosition := args.TTL == 0
	uniqueActorID := fmt.Sprintf("%s_%s_%s", args.ActorID, args.ActorType, args.Domain)
	//1. 先到LRU里面去找, 找到检查server存在性, 存在就返回
	//2. 到remote里面找, 找到检查sever存在性, 存在, 写LUR返回
	//3. 上锁
	//4. 到LRU里面找, 到remote里面找, 检查存在性, 存在写LRU返回
	//5. 按照domain, type分配一个server, 然后写remote, 写LRU, 返回
	position, err := this.findPositionFromRemote(uniqueActorID, needAllocNewPosition)
	if err != nil {
		return nil, err
	}
	if position != nil {
		return position, nil
	}
	mutex, err := util.NewMutex(this.server.GetEtcdClient(), uniqueActorID)
	if err != nil {
		return nil, errors.WithStack(err)
	}
	err = mutex.Lock()
	if err != nil {
		return nil, errors.WithStack(err)
	}
	defer mutex.AsyncClose()

	position = this.findPositionInLRU(uniqueActorID, needAllocNewPosition)
	if position != nil {
		return position, nil
	}
	position, err = this.findPositionFromRemote(uniqueActorID, needAllocNewPosition)
	if err != nil {
		return nil, err
	}
	if position != nil {
		this.savePositionToLRU(uniqueActorID, position)
		return position, nil
	}

	//随机生成一个server
	newServerID := this.chooseServerByRandom(args.Domain, args.ActorType)
	log.Info("chooseServerByRandom", zap.String("ActorID", uniqueActorID), zap.Int64("ServerID", newServerID))
	if newServerID < 0 {
		if newServerID == -1 {
			return nil, errors.New("without enough server")
		}
		if newServerID == -2 {
			return nil, errors.New("not find actor type")
		}
		return nil, errors.New("unknown chooseSever error")
	}

	position = &ActorPositionInfo{
		ActorID:    args.ActorID,
		ActorType:  args.ActorType,
		Domain:     args.Domain,
		TTL:        args.TTL,
		CreateTime: util.GetMilliSeconds(),
		ServerID:   newServerID,
	}
	err = this.savePositionToRemote(uniqueActorID, position)
	if err != nil {
		return nil, err
	}
	this.savePositionToLRU(uniqueActorID, position)

	return position, nil
}

func (this *ActorMembership) FindPosition(args *ActorPositionArgs) (*ActorPositionInfo, error) {
	needAllocNewPosition := args.TTL == 0
	uniqueActorID := fmt.Sprintf("%s_%s_%s", args.ActorID, args.ActorType, args.Domain)
	position := this.findPositionInLRU(uniqueActorID, needAllocNewPosition)
	if position != nil {
		return position, nil
	}
	return this.findOrAllocNewPosition(args)
}

func (this *ActorMembership) DeletePosition(args *ActorPositionArgs) error {
	uniqueActorID := fmt.Sprintf("%s_%s_%s", args.ActorID, args.ActorType, args.Domain)

	mutex, err := util.NewMutex(this.server.GetEtcdClient(), uniqueActorID)
	if err != nil {
		return err
	}
	mutex.Lock()
	go mutex.AsyncClose()
	this.actorPositionCache.Remove(uniqueActorID)
	err = this.removePositionFromRemote(uniqueActorID)
	if err != nil {
		return err
	}
	return nil
}
