package server

import (
	"fmt"
	lru "github.com/hashicorp/golang-lru"
	"github.com/pingcap/log"
	"go.etcd.io/etcd/clientv3/concurrency"
	"go.uber.org/zap"
	"pd/server/util"
	"sort"
)

const RecentActorMemberServerIDLRUSize = 1024 * 10
const ActorPositionLRUSize = 1024 * 1024
const PDServerHeartBeatTime int64 = 3 * 1000
const ActorHostEventLifeTime = 60 * 1000

//路径: /actor_server/{server_id}
//内容是ActorHostInfo的JSON字符串
const ActorHostServerPrefix = "/actor_server"

func GenerateServerKey(serverID int64) string {
	return fmt.Sprintf("%s/%d", ActorHostServerPrefix, serverID)
}

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
	types map[string]map[int64]*ActorHostInfo //Domain:ActorType => Set<ActorHostInfo>
}

type MutexFactory func(string, ...concurrency.SessionOption) (*util.EtcdMutex, error)

type ActorMembership struct {
	lastUpdateIndexTime int64
	lastGCEventTime     int64
	index               *ActorHostIndex
	events              map[int64]*ActorHostAddRemoveEvent //milliSeconds => events
	eventsSnapshot      []*ActorHostAddRemoveEvent         //event snapshot
	registeredID        *lru.Cache                         //recent registered server id
	actorPositionCache  *lru.Cache                         //actor position lru
	mutexFunc           MutexFactory                       //etcd mutex factory
}

type ActorHostAddRemoveEvent struct {
	Time   int64
	Add    []int64
	Remove []int64
}

type ActorHostEventSlice []*ActorHostAddRemoveEvent

func (s ActorHostEventSlice) Len() int {
	return len(s)
}

func (s ActorHostEventSlice) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}

func (s ActorHostEventSlice) Less(i, j int) bool {
	return s[i].Time < s[j].Time
}

func NewActorMembershipManager(f MutexFactory) *ActorMembership {
	index := buildIndexFromArray(nil)
	serverIdLru, _ := lru.New(RecentActorMemberServerIDLRUSize)
	actorPositionLru, _ := lru.New(ActorPositionLRUSize)
	return &ActorMembership{
		lastUpdateIndexTime: util.GetMilliSeconds(),
		lastGCEventTime:     util.GetMilliSeconds(),
		index:               index,
		events:              map[int64]*ActorHostAddRemoveEvent{},
		registeredID:        serverIdLru,
		actorPositionCache:  actorPositionLru,
		mutexFunc:           f,
	}
}

func (this *ActorMembership) compare2Index(newIndex *ActorHostIndex) ([]int64, []int64) {
	add := make([]int64, 0)
	remove := make([]int64, 0)
	for k, v := range newIndex.ids {
		if _, ok := this.index.ids[k]; !ok {
			add = append(add, v.ServerID)
		}
	}
	for k, v := range this.index.ids {
		if _, ok := newIndex.ids[k]; !ok {
			remove = append(remove, v.ServerID)
		}
	}
	return add, remove
}

func (this *ActorMembership) updateEvents(add []int64, remove []int64) {
	if len(add) > 0 || len(remove) > 0 {
		event := &ActorHostAddRemoveEvent{
			Time:   this.lastUpdateIndexTime,
			Add:    add,
			Remove: remove,
		}
		this.events[this.lastUpdateIndexTime] = event
	}

	needRemove := make([]int64, 0)
	snapshot := make([]*ActorHostAddRemoveEvent, 0)
	currentTime := util.GetMilliSeconds()
	for k, v := range this.events {
		if currentTime-k >= ActorHostEventLifeTime {
			needRemove = append(needRemove, k)
		} else {
			snapshot = append(snapshot, v)
		}
	}

	sort.Sort(ActorHostEventSlice(snapshot))
	this.eventsSnapshot = snapshot

	for _, v := range needRemove {
		delete(this.events, v)
	}
}

func (this *ActorMembership) GetReadonlyIndex() *ActorHostIndex {
	return this.index
}

func (this *ActorMembership) GetReadonlyRecentEvents() []*ActorHostAddRemoveEvent {
	return this.eventsSnapshot
}

func (this *ActorMembership) AddActorMemberID(serverID int64) {
	this.registeredID.Add(serverID, serverID)
}

func (this *ActorMembership) GetActorMemberID(serverID int64) interface{} {
	v, _ := this.registeredID.Get(serverID)
	return v
}

func (this *ActorMembership) GetActorMemberInfoByID(serverID int64) *ActorHostInfo {
	index := this.GetReadonlyIndex()
	info, _ := index.ids[serverID]
	return info
}

func (this *ActorMembership) GetActorMembersByDomain(domain string) map[int64]*ActorHostInfo {
	result := map[int64]*ActorHostInfo{}
	index := this.GetReadonlyIndex()

	for k, v := range index.ids {
		if v.Domain == domain {
			result[k] = v
		}
	}

	return result
}

func (this *ActorMembership) GetMembersByDomainAndType(domain string, actorType string) map[int64]*ActorHostInfo {
	typesName := domain + ":" + actorType
	index := this.GetReadonlyIndex()

	result, ok := index.types[typesName]
	if ok {
		return map[int64]*ActorHostInfo{}
	}
	return result
}

func (this *ActorMembership) UpdateIndex(startTime int64, newIndex *ActorHostIndex) ([]int64, []int64) {
	if startTime > this.lastUpdateIndexTime {
		this.lastUpdateIndexTime = startTime

		add, remove := this.compare2Index(newIndex)
		this.index = newIndex
		this.updateEvents(add, remove)
		return add, remove
	}
	log.Warn("UpdateIndex", zap.Int64("LastUpdateTime", this.lastUpdateIndexTime), zap.Int64("StartTime", startTime))
	return nil, nil
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
			var realTypeName = v.Domain + ":" + typeName
			if _, ok := index.types[realTypeName]; !ok {
				index.types[realTypeName] = map[int64]*ActorHostInfo{}
			}
			s := index.types[realTypeName]
			s[v.ServerID] = v
		}
	}
	return index
}
