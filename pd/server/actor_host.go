package server

import (
	"github.com/pingcap/log"
	"github.com/pkg/errors"
	"go.uber.org/zap"
)

type ActorHostInfo struct {
	ServerID  int64    `json:"server_id"`  //服务器ID
	LeaseID   int64    `json:"lease_id"`   //租约的ID
	Load      int64    `json:"load"`       //负载
	StartTime int64    `json:"start_time"` //服务器注册的时间(单位毫秒)
	TTL       int64    `json:"ttl"`        //存活时间(心跳时间*3)
	Domain    string   `json:"domain"`     //命名空间
	ActorType []string `json:"actor_type"` //可以提供服务的类型
}

type ActorHostSnapshot struct {
	hosts map[int64]*ActorHostInfo    //ServerID => ActorHostInfo
	types map[string][]*ActorHostInfo //ActorType => List<ActorHostInfo>
}

func (this *Server) GetActorHostByID(serverID int64) *ActorHostInfo {
	this.hostMutex.Lock()
	defer this.hostMutex.Unlock()
	server, _ := this.hosts[serverID]
	return server
}

func (this *Server) GetActorHost() []*ActorHostInfo {
	this.hostMutex.Lock()
	defer this.hostMutex.Unlock()

	var hosts []*ActorHostInfo
	for _, v := range this.hosts {
		hosts = append(hosts, v)
	}
	return hosts

}

func (this *Server) TryAddActorHost(info *ActorHostInfo) error {
	{
		this.hostMutex.Lock()
		defer this.hostMutex.Unlock()

		if _, ok := this.hosts[info.ServerID]; ok {
			return errors.Errorf("ServerID:%v exist", info.ServerID)
		}

		this.hosts[info.ServerID] = info
	}
	log.Info("AddActorHost", zap.Int64("ServerID", info.ServerID))
	return nil
}

//需要构造:
//ServerID => ActorHostInfo
//ActorType => List(ActorHostInfo)
func (this *Server) TryUpdateActorHostList() error {
	//TODO:
	//从etcd pull所有的服务器信息
	//构造map, 然后替换hosts
	//需要注意分配程序, 可能新的服务器会丢失一次: Add了Host, 然后pull的时候还没进etcd
	return nil
}
