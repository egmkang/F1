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
	Hosts map[int64]*server.ActorHostInfo `json:"hosts"`
}

func generateServerKey(serverID int64, domain string) string {
	return fmt.Sprintf("%s/%s/%d", server.ActorHostServerPrefix, domain, serverID)
}

func (this *serverHandler) getActorHostInfoByID(serverID int64, domain string) *server.ActorHostInfo {
	key := generateServerKey(serverID, domain)
	data, err := util.EtcdGetKVValue(this.server.GetEtcdClient(), key)
	if err != nil || data == nil {
		return nil
	}

	info := &server.ActorHostInfo{}
	err = util.ReadJSONFromData(data, info)
	if err != nil {
		log.Error("GetActorHostInfoByID", zap.Int64("ServerID", serverID), zap.String("Domain", domain), zap.Error(err))
		return nil
	}
	return info
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

	//从etcd里面获取server信息, 如果存在就拒绝注册
	if info := this.getActorHostInfoByID(serverInfo.ServerID, serverInfo.Domain); info != nil {
		this.render.JSON(w, http.StatusBadRequest, fmt.Sprintf("RegisterNewServer, ServerID:%d exist", serverInfo.ServerID))
		log.Info("RegisterNewServer server exist", zap.Int64("ServerID", info.ServerID), zap.Int64("LeaseID", info.LeaseID))
		return
	}

	//从LRU里面查看
	if v := this.server.GetActorHostID(serverInfo.ServerID); v != nil {
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

	json, err := util.JSON(serverInfo)
	if err != nil {
		this.render.JSON(w, http.StatusInternalServerError, err.Error())
		return
	}

	//domain和server id存在正交, 实际上是server id决定了domain
	key := generateServerKey(serverInfo.ServerID, serverInfo.Domain)
	_, err = util.EtcdKVPut(this.server.GetEtcdClient(), key, json, clientv3.WithLease(lease.ID))
	if err != nil {
		this.render.JSON(w, http.StatusInternalServerError, err.Error())
		return
	}

	this.server.AddActorHostID(serverInfo.ServerID)

	log.Info("RegisterNewServer",
		zap.Int64("ServerID", serverInfo.ServerID),
		zap.Int64("LeaseID", serverInfo.LeaseID),
		zap.Int64("TTL", serverInfo.TTL),
		zap.Int64("StartTime", serverInfo.StartTime),
		zap.String("domain", serverInfo.Domain),
		zap.String("Address", serverInfo.Address),
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

	_, err := util.EtcdLeaseKeepAliveOnce(this.server.GetEtcdClient(), serverInfo.LeaseID)
	if err != nil {
		this.render.JSON(w, http.StatusBadRequest, err.Error())
		return
	}

	//TODO:
	//更新缓存和etcd里面的数据

	log.Debug("KeepAliveServer",
		zap.Int64("ServerID", serverInfo.ServerID),
		zap.Int64("LeaseID", serverInfo.LeaseID),
		zap.Int64("Load", serverInfo.Load))

	hosts := this.server.GetActorHosts()
	this.render.JSON(w, http.StatusOK, &KeepAliveServerResp{Hosts: hosts})
}
