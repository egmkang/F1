package api

import (
	"fmt"
	"github.com/pingcap/log"
	"github.com/unrolled/render"
	"go.etcd.io/etcd/clientv3"
	"go.uber.org/zap"
	"net/http"
	"pd/server"
	"pd/server/util"
)

//路径: /actor_server/{domain}/{server_id}
//内容是ActorHostInfo的JSON字符串
const ActorHostServerPrefix = "/actor_server"

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
	hosts []*server.ActorHostInfo `json:"hosts"`
}

func (this *serverHandler) RegisterNewServer(w http.ResponseWriter, r *http.Request) {
	serverInfo := &server.ActorHostInfo{}
	if err := util.ReadJSONResponseError(this.render, w, r.Body, serverInfo); err != nil {
		return
	}

	if len(serverInfo.Domain) == 0 {
		log.Error("RegisterNewServer domain is null", zap.Int64("ServerID", serverInfo.ServerID))
		this.render.JSON(w, http.StatusBadRequest, "RegisterNewServer domain is null")
		return
	}

	//TODO:
	//从etcd里面获取server信息, 如果存在就拒绝注册

	host := this.server.GetActorHostByID(serverInfo.ServerID)

	if host != nil {
		log.Error("RegisterNewServer ServerID exists", zap.Int64("ServerID", serverInfo.ServerID))
		this.render.JSON(w, http.StatusBadRequest, "RegisterNewServer ServerID exists")
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

	json, err := util.JSON(serverInfo)
	if err != nil {
		this.render.JSON(w, http.StatusInternalServerError, err.Error())
		return
	}

	key := fmt.Sprintf("%s/%s/%d", ActorHostServerPrefix, serverInfo.Domain, serverInfo.ServerID)
	_, err = util.EtcdKVPut(this.server.GetEtcdClient(), key, json, clientv3.WithLease(lease.ID))
	if err != nil {
		this.render.JSON(w, http.StatusInternalServerError, err.Error())
		return
	}

	log.Info("RegisterNewServer",
		zap.Int64("ServerID", serverInfo.ServerID),
		zap.Int64("LeaseID", serverInfo.LeaseID),
		zap.Int64("TTL", serverInfo.TTL),
		zap.Int64("StartTime", serverInfo.StartTime),
		zap.String("domain", serverInfo.Domain),
		zap.Strings("ActorType", serverInfo.ActorType))

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

	resp, err := util.EtcdLeaseKeepAliveOnce(this.server.GetEtcdClient(), serverInfo.LeaseID)
	if err != nil {
		this.render.JSON(w, http.StatusBadRequest, err.Error())
		return
	}

	print(resp.ID, resp.TTL)

	log.Debug("KeepAliveServer",
		zap.Int64("ServerID", serverInfo.ServerID),
		zap.Int64("LeaseID", serverInfo.LeaseID),
		zap.Int64("Load", serverInfo.TTL))

	hosts := this.server.GetActorHost()
	this.render.JSON(w, http.StatusOK, &KeepAliveServerResp{hosts: hosts})
}
