package api

import (
	"github.com/gorilla/mux"
	"github.com/unrolled/render"
	"net/http"
	"pd/server"
)

const ApiPrefix = "/pd"

func createRouter(prefix string, server *server.Server) *mux.Router {
	render := render.New(render.Options{IndentJSON: true})

	subRouter := mux.NewRouter().PathPrefix(prefix).Subrouter()

	infoHandler := newInfoHandler(server, render)
	subRouter.HandleFunc("/api/v1/version", infoHandler.Version).Methods("GET")

	idHandler := newIdHandler(server, render)
	subRouter.HandleFunc("/api/v1/id/new_server_id", idHandler.NewServerID).Methods("POST")
	subRouter.HandleFunc("/api/v1/id/new_sequence/{sequence_key}/{step}", idHandler.NewSequenceID).Methods("POST")

	serverHandler := newServerHandler(server, render)
	subRouter.HandleFunc("/api/v1/server/register", serverHandler.RegisterNewServer).Methods("POST")
	subRouter.HandleFunc("/api/v1/server/keep_alive", serverHandler.KeepAliveServer).Methods("POST")
	return subRouter
}

func NewHandle(server *server.Server) http.Handler {
	return createRouter(ApiPrefix, server)
	//engine := negroni.New()

	//router := mux.NewRouter()
	//router.PathPrefix(ApiPrefix).Handler(negroni.New(
	//	negroni.Wrap(createRouter(ApiPrefix, server)),
	//))

	//engine.UseHandler(router)
	//return engine
}
