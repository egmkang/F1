package server

import (
	"github.com/pingcap/log"
	"go.etcd.io/etcd/clientv3"
	"go.uber.org/zap"
	"pd/server/util"
	"time"
)

const PDServerHeartBeatTime int64 = 3 * 1000

//路径: /actor_server/{domain}/{server_id}
//内容是ActorHostInfo的JSON字符串
const ActorHostServerPrefix = "/actor_server"

type ActorHostInfo struct {
	ServerID  int64    `json:"server_id"`  //服务器ID
	LeaseID   int64    `json:"lease_id"`   //租约的ID
	Load      int64    `json:"load"`       //负载
	StartTime int64    `json:"start_time"` //服务器注册的时间(单位毫秒)
	TTL       int64    `json:"ttl"`        //存活时间(心跳时间*3)
	Address   string   `json:"address"`    //服务器地址
	Domain    string   `json:"domain"`     //命名空间
	ActorType []string `json:"actor_type"` //可以提供服务的类型
}

type ActorHostIndex struct {
	ids   map[int64]*ActorHostInfo            //ServerID => ActorHostInfo
	types map[string]map[int64]*ActorHostInfo //ActorType => Set<ActorHostInfo>
}

type ActorHostManager struct {
	lastUpdateTime int64
	index          *ActorHostIndex
}

func NewActorHostManager() *ActorHostManager {
	index := buildIndexFromArray(nil)
	return &ActorHostManager{lastUpdateTime: util.GetMilliSeconds(), index: index}
}

func (this *ActorHostManager) compare2Index(newIndex *ActorHostIndex) ([]*ActorHostInfo, []*ActorHostInfo) {
	add := make([]*ActorHostInfo, 0)
	remove := make([]*ActorHostInfo, 0)
	for k, v := range newIndex.ids {
		if _, ok := this.index.ids[k]; !ok {
			add = append(add, v)
		}
	}
	for k, v := range this.index.ids {
		if _, ok := newIndex.ids[k]; !ok {
			remove = append(remove, v)
		}
	}
	return add, remove
}

func (this *ActorHostManager) UpdateIndex(startTime int64, newIndex *ActorHostIndex) ([]*ActorHostInfo, []*ActorHostInfo) {
	if startTime > this.lastUpdateTime {
		this.lastUpdateTime = startTime

		add, remove := this.compare2Index(newIndex)
		this.index = newIndex
		return add, remove
	}
	log.Warn("UpdateIndex", zap.Int64("LastUpdateTime", this.lastUpdateTime), zap.Int64("StartTime", startTime))
	return nil, nil
}

func (this *Server) GetActorHosts() map[int64]*ActorHostInfo {
	return this.actorHostManager.index.ids
}

func (this *Server) updateActorHostListLoop() {
	beginTime := util.GetMilliSeconds()
	for i := int64(0); ; i++ {
		currentTime := util.GetMilliSeconds()
		sleepTime := beginTime + (i+1)*PDServerHeartBeatTime - currentTime
		time.Sleep(time.Millisecond * time.Duration(sleepTime))
		go this.tryUpdateActorHostListOnce()
	}
}

func tryParseActorHostInfo(data []byte) *ActorHostInfo {
	info := &ActorHostInfo{}
	err := util.ReadJSONFromData(data, info)
	if err != nil {
		log.Error("tryParseActorHostInfo fail", zap.Error(err))
		return nil
	}
	return info
}

func buildIndexFromArray(list []*ActorHostInfo) *ActorHostIndex {
	index := &ActorHostIndex{ids: map[int64]*ActorHostInfo{}, types: map[string]map[int64]*ActorHostInfo{}}
	if list == nil || len(list) == 0 {
		return index
	}
	for _, v := range list {
		//id的索引
		index.ids[v.ServerID] = v
		//注册类型的索引
		for _, typeName := range v.ActorType {
			if _, ok := index.types[typeName]; !ok {
				index.types[typeName] = map[int64]*ActorHostInfo{}
			}
			s := index.types[typeName]
			s[v.ServerID] = v
		}
	}
	return index
}

func (this *Server) onAddNewActorHost(host *ActorHostInfo) {

}

func (this *Server) onRemoveActorHost(host *ActorHostInfo) {

}

func (this *Server) compare2Index(newIndex *ActorHostIndex) {

}

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
	add, remove := this.actorHostManager.UpdateIndex(startTime, index)
	if add != nil && len(add) > 0 {
		log.Info("TryUpdateActorHostList", zap.Reflect("AddServer", add))
	}
	if remove != nil && len(remove) > 0 {
		log.Info("TryUpdateActorHostList", zap.Reflect("RemoveServer", remove))
	}
}
