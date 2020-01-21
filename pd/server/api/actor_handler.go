package api

import (
	"github.com/unrolled/render"
	"net/http"
	"pd/server"
	"pd/server/util"
)

const ActorTokenPathPrefix = "/global/actor_token"

var actorTokenID = util.NewIdGenerator(ActorTokenPathPrefix, 1000)

type actorHandler struct {
	server *server.Server
	render *render.Render
}

func newActorHandler(server *server.Server, render *render.Render) *actorHandler {
	return &actorHandler{server: server, render: render}
}

type FindActorPositionRequest struct {
	Domain    string `json:"domain"`
	ActorType string `json:"actor_type"`
	ActorID   string `json:"actor_id"`
	TTL       int64  `json:"ttl"`
}

type FindActorPositionResponse struct {
	ActorID       string `json:"actor_id"`
	ActorType     string `json:"actor_type"`
	Domain        string `json:"domain"`
	TTL           int64  `json:"ttl"`
	CreateTime    int64  `json:"create_time"`
	ServerID      int64  `json:"server_id"`
	ServerAddress string `json:"server_address"`
}

func (this *actorHandler) FindPosition(w http.ResponseWriter, r *http.Request) {
	req := &FindActorPositionRequest{}
	if err := util.ReadJSONResponseError(this.render, w, r.Body, req); err != nil {
		return
	}

	if len(req.Domain) == 0 || len(req.ActorType) == 0 || len(req.ActorID) == 0 {
		this.render.JSON(w, http.StatusBadRequest, "args error")
		return
	}
	args := &server.ActorPositionArgs{
		ActorID:   req.ActorID,
		ActorType: req.ActorType,
		Domain:    req.Domain,
		TTL:       req.TTL,
	}

	result, err := this.server.GetActorMembership().FindPosition(args)
	if err != nil {
		this.render.JSON(w, http.StatusBadRequest, err.Error())
		return
	}

	serverInfo := this.server.GetActorHostInfoByServerID(result.ServerID)
	if serverInfo == nil {
		this.render.JSON(w, http.StatusBadRequest, "server not found")
		return
	}
	resp := FindActorPositionResponse{
		ActorID:       result.ActorID,
		ActorType:     result.ActorType,
		Domain:        result.Domain,
		TTL:           result.TTL,
		CreateTime:    result.CreateTime,
		ServerID:      result.ServerID,
		ServerAddress: serverInfo.Address,
	}

	this.render.JSON(w, http.StatusOK, resp)
}

func (this *actorHandler) NewActorToken(w http.ResponseWriter, r *http.Request) {
	newId, err := actorTokenID.GetNewID(this.server.GetEtcdClient())
	if err != nil {
		this.render.JSON(w, http.StatusInternalServerError, err.Error())
		return
	}
	info := NewIDResp{ID: newId}
	this.render.JSON(w, http.StatusOK, info)
}

type DeleteActorResponse struct {
	ActorID   string `json:"actor_id"`
	Domain    string `json:"domain"`
	ActorType string `json:"actor_type"`
}

func (this *actorHandler) DeleteActor(w http.ResponseWriter, r *http.Request) {
	req := &FindActorPositionRequest{}
	if err := util.ReadJSONResponseError(this.render, w, r.Body, req); err != nil {
		return
	}

	if len(req.Domain) == 0 || len(req.ActorType) == 0 || len(req.ActorID) == 0 {
		this.render.JSON(w, http.StatusBadRequest, "args error")
		return
	}
	args := &server.ActorPositionArgs{
		ActorID:   req.ActorID,
		ActorType: req.ActorType,
		Domain:    req.Domain,
	}
	err := this.server.GetActorMembership().DeletePosition(args)
	if err != nil {
		this.render.JSON(w, http.StatusBadRequest, err.Error())
		return
	}

	//TODO:
	//要广播给其他进程

	resp := &DeleteActorResponse{
		ActorID:   args.ActorID,
		Domain:    args.Domain,
		ActorType: args.ActorType,
	}
	this.render.JSON(w, http.StatusOK, resp)
}
