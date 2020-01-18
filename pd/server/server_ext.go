package server

import (
	"github.com/pingcap/log"
	"go.etcd.io/etcd/clientv3"
	"go.uber.org/zap"
	"pd/server/util"
	"time"
)

//从etcd pull所有的服务器信息
//构造map, 然后替换hosts
//需要注意分配程序, 可能新的服务器会丢失一次: Add了Host, 然后pull的时候还没进etcd
func (this *Server) tryUpdateActorHostListOnce() {
	startTime := util.GetMilliSeconds()
	prefix := ActorHostServerPrefix
	resp, err := util.EtcdKVGet(this.etcdClient, prefix, clientv3.WithPrefix())
	if err != nil {
		log.Error("tryUpdateActorHostListOnce", zap.Error(err))
		return
	}

	var list []*ActorHostInfo
	for _, data := range resp.Kvs {
		item := tryParseActorHostInfo(data.Value)
		if item != nil {
			list = append(list, item)
		}
	}

	index := buildIndexFromArray(list)
	add, remove := this.actorMembership.UpdateIndex(startTime, index)
	if add != nil && len(add) > 0 {
		log.Info("TryUpdateActorHostList", zap.Reflect("AddServer", add))
	}
	if remove != nil && len(remove) > 0 {
		log.Info("TryUpdateActorHostList", zap.Reflect("RemoveServer", remove))
	}
}

func (this *Server) GetActorMembershipRecentEvent() []*ActorHostAddRemoveEvent {
	return this.actorMembership.GetReadonlyRecentEvents()
}

func (this *Server) SaveActorHostInfo(serverInfo *ActorHostInfo) error {
	json, err := util.JSON(serverInfo)
	if err != nil {
		return err
	}

	key := GenerateServerKey(serverInfo.ServerID)
	_, err = util.EtcdKVPut(this.etcdClient, key, json, clientv3.WithLease(clientv3.LeaseID(serverInfo.LeaseID)))
	if err != nil {
		return err
	}
	return nil
}

func (this *Server) GetActorHostInfoByServerID(serverID int64) *ActorHostInfo {
	index := this.actorMembership.GetReadonlyIndex()
	info, ok := index.ids[serverID]
	if ok {
		return info
	}

	key := GenerateServerKey(serverID)
	data, err := util.EtcdGetKVValue(this.etcdClient, key)
	if err != nil || data == nil {
		return nil
	}
	info = tryParseActorHostInfo(data)
	return info
}

func (this *Server) GetActorMembersByDomain(domain string) map[int64]*ActorHostInfo {
	return this.actorMembership.GetActorMembersByDomain(domain)
}

func (this *Server) GetActorMembersByDomainAndType(domain string, actorType string) map[int64]*ActorHostInfo {
	return this.actorMembership.GetMembersByDomainAndType(domain, actorType)
}

func (this *Server) AddActorHostID(serverID int64) {
	this.actorMembership.AddActorMemberID(serverID)
}

func (this *Server) GetRegisteredActorHostID(serverID int64) interface{} {
	v := this.actorMembership.GetActorMemberID(serverID)
	return v
}

func (this *Server) updateActorHostListLoop() {
	beginTime := util.GetMilliSeconds()
	for i := int64(0); ; i++ {
		go this.tryUpdateActorHostListOnce()
		currentTime := util.GetMilliSeconds()
		sleepTime := beginTime + (i+1)*PDServerHeartBeatTime - currentTime
		time.Sleep(time.Millisecond * time.Duration(sleepTime))
	}
}
