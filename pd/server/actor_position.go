package server

import (
	"fmt"
	"github.com/pkg/errors"
	"pd/server/util"
)

const AliveServerTime = 15 * 1000 //起码要存活超过15秒以上, 才会被系统分配到Actor

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

}

func (this *ActorMembership) findPositionInRemote(uniqueActorID string, needAllocNewPosition bool) *ActorPositionInfo {
	return nil
}

func (this *ActorMembership) savePositionToRemote(uniqueActorID string, position *ActorPositionInfo) {

}

func (this *ActorMembership) chooseServerByRandom(domain string, actorType string) int64 {
	//返回-1表示服务器个数不够
}

func (this *ActorMembership) findOrAllocNewPosition(args *ActorPositionArgs) (*ActorPositionInfo, error) {
	needAllocNewPosition := args.TTL == 0
	uniqueActorID := fmt.Sprint("%s_%s_%s", args.ActorID, args.ActorType, args.Domain)
	//1. 先到LRU里面去找, 找到检查server存在性, 存在就返回
	//2. 到remote里面找, 找到检查sever存在性, 存在, 写LUR返回
	//3. 上锁
	//4. 到LRU里面找, 到remote里面找, 检查存在性, 存在写LRU返回
	//5. 按照domain, type分配一个server, 然后写remote, 写LRU, 返回
	position := this.findPositionInRemote(uniqueActorID, needAllocNewPosition)
	if position != nil {
		return position, nil
	}
	mutex, err := this.mutexFunc(uniqueActorID)
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
	position = this.findPositionInRemote(uniqueActorID, needAllocNewPosition)
	if position != nil {
		this.savePositionToLRU(uniqueActorID, position)
		return position, nil
	}

	//随机生成一个server
	newServerID := this.chooseServerByRandom(args.Domain, args.ActorType)
	if newServerID < 0 {
		return nil, errors.Errorf("without enough server")
	}

	position = &ActorPositionInfo{
		ActorID:    args.ActorID,
		ActorType:  args.ActorType,
		Domain:     args.Domain,
		TTL:        args.TTL,
		CreateTime: util.GetMilliSeconds(),
		ServerID:   newServerID,
	}
	this.savePositionToLRU(uniqueActorID, position)
	this.savePositionToRemote(uniqueActorID, position)
	return position, nil
}

func (this *ActorMembership) findPosition(args *ActorPositionArgs) (*ActorPositionInfo, error) {
	needAllocNewPosition := args.TTL == 0
	uniqueActorID := fmt.Sprint("%s_%s_%s", args.ActorID, args.ActorType, args.Domain)
	position := this.findPositionInLRU(uniqueActorID, needAllocNewPosition)
	if position != nil {
		return position, nil
	}
	return this.findOrAllocNewPosition(args)
}
