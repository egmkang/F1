package api

import (
	"fmt"
	"github.com/pingcap/log"
	"github.com/unrolled/render"
	"go.uber.org/zap"
	"net/http"
	"pd/server"
	"pd/server/util"
	"strings"
)

//默认存活时间是15秒, 5秒一个心跳
const ActorHostServerDefaultTTL = 15

type serverHandler struct {
	server *server.Server
	render *render.Render
}

func newServerHandler(server *server.Server, render *render.Render) *serverHandler {
	return &serverHandler{server: server, render: render}
}

type RegisterNewServerResp struct {
	LeaseID int64 `json:"lease_id"`
}

type KeepAliveServerResp struct {
	Hosts  map[int64]*server.ActorHostInfo   `json:"hosts"`
	Events []*server.ActorHostAddRemoveEvent `json:"events"`
}

func (this *serverHandler) RegisterNewServer(w http.ResponseWriter, r *http.Request) {
	serverInfo := &server.ActorHostInfo{}
	if err := util.ReadJSONResponseError(this.render, w, r.Body, serverInfo); err != nil {
		return
	}

	//从etcd里面获取server信息, 如果存在就拒绝注册
	if info := this.server.GetActorHostInfoByServerID(serverInfo.ServerID); info != nil {
		this.render.JSON(w, http.StatusBadRequest, fmt.Sprintf("RegisterNewServer, ServerID:%d exist", serverInfo.ServerID))
		log.Info("RegisterNewServer server exist", zap.Int64("ServerID", info.ServerID), zap.Int64("LeaseID", info.LeaseID))
		return
	}

	//从LRU里面查看
	if v := this.server.GetRegisteredActorHostID(serverInfo.ServerID); v != nil {
		this.render.JSON(w, http.StatusBadRequest, fmt.Sprintf("RegisterNewServer, ServerID:%d exist", serverInfo.ServerID))
		log.Info("RegisterNewServer server exist", zap.Reflect("ServerID", v))
		return
	}

	serverInfo.StartTime = util.GetMilliSeconds()
	if serverInfo.TTL == 0 {
		serverInfo.TTL = ActorHostServerDefaultTTL
	}

	lease, err := util.EtcdLeaseGrant(this.server.GetEtcdClient(), serverInfo.TTL)
	if err != nil {
		this.render.JSON(w, http.StatusInternalServerError, err.Error())
		return
	}
	serverInfo.LeaseID = int64(lease.ID)

	err = this.server.SaveActorHostInfo(serverInfo)
	if err != nil {
		this.render.JSON(w, http.StatusInternalServerError, err.Error())
		return
	}

	this.server.AddActorHostID(serverInfo.ServerID)

	var sb = strings.Builder{}
	for _, s := range serverInfo.Services {
		if len(s.ImplType) == 0 {
			continue
		}
		sb.WriteString(s.ActorType)
		sb.WriteString(" => ")
		sb.WriteString(s.ImplType)
		sb.WriteString(", ")
	}

	var services = sb.String()
	if len(services) > 2 {
		services = services[0 : len(services)-2]
	}

	log.Info("RegisterNewServer",
		zap.Int64("ServerID", serverInfo.ServerID),
		zap.Int64("LeaseID", serverInfo.LeaseID),
		zap.Int64("TTL", serverInfo.TTL),
		zap.Int64("StartTime", serverInfo.StartTime),
		zap.String("Address", serverInfo.Address),
		zap.String("Services", services))

	data := &RegisterNewServerResp{LeaseID: serverInfo.LeaseID}
	this.render.JSON(w, http.StatusOK, data)
}

func (this *serverHandler) KeepAliveServer(w http.ResponseWriter, r *http.Request) {
	serverInfo := &server.ActorHostInfo{}
	if err := util.ReadJSONResponseError(this.render, w, r.Body, serverInfo); err != nil {
		return
	}

	if serverInfo.ServerID == 0 || serverInfo.LeaseID == 0 {
		this.render.JSON(w, http.StatusBadRequest, "ServerID/LeaseID must input")
		return
	}

	_, err := util.EtcdLeaseKeepAliveOnce(this.server.GetEtcdClient(), serverInfo.LeaseID)
	if err != nil {
		this.render.JSON(w, http.StatusBadRequest, err.Error())
		return
	}

	//更新缓存和etcd里面的数据
	info := this.server.GetActorHostInfoByServerID(serverInfo.ServerID)
	if info == nil {
		log.Error("KeepAliveServer server not found", zap.Int64("ServerID", serverInfo.ServerID))
		this.render.JSON(w, http.StatusBadRequest, "ServerID not found")
		return
	}

	info.Load = serverInfo.Load
	this.server.SaveActorHostInfo(info)

	log.Debug("KeepAliveServer",
		zap.Int64("ServerID", info.ServerID),
		zap.Int64("LeaseID", info.LeaseID),
		zap.Int64("Load", info.Load))

	result := &KeepAliveServerResp{
		Hosts:  this.server.GetActorMembers(),
		Events: this.server.GetActorMembershipRecentEvent(),
	}
	this.render.JSON(w, http.StatusOK, result)
}
