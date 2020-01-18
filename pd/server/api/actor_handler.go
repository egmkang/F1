package api

import (
	"github.com/unrolled/render"
	"net/http"
	"pd/server"
)

type actorHandler struct {
	server *server.Server
	render *render.Render
}

func newActorHandler(server *server.Server, render *render.Render) *actorHandler {
	return &actorHandler{server: server, render: render}
}

type FindActorPositionRequest struct {
	Domain     string `json:"domain"`
	ActorType  string `json:"actor_type"`
	ActorID    string `json:"actor_id"`
	TTL        int64  `json:"ttl"`
	NeedCreate bool   `json:"need_create"`
}

type FindActorPositionResponse struct {
	ActorID    string `json:"actor_id"`
	ActorType  string `json:"actor_type"`
	Domain     string `json:"domain"`
	TTL        int64  `json:"ttl"`
	CreateTime int64  `json:"create_time"`
	ServerID   int64  `json:"server_id"`
}

func (this *actorHandler) findPosition() {

}

func (this *actorHandler) FindPosition(w http.ResponseWriter, r *http.Request) {

}
