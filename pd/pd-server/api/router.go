package api

import (
	"github.com/gorilla/mux"
	"github.com/unrolled/render"
	"github.com/urfave/negroni"
	"net/http"
	"pd/pd-server/server"
)

const apiPrefix = "/pd"

func createRouter(prefix string, server *server.Server) *mux.Router {
	render := render.New(render.Options{IndentJSON: true})

	router := mux.NewRouter().PathPrefix(prefix).Subrouter()

	infoHandler := newInfoHandler(server, render)
	router.HandleFunc("/api/v1/version", infoHandler.Version).Methods("GET")

	return router
}

func NewHandle(server *server.Server) http.Handler {
	engine := negroni.New()

	recovery := negroni.NewRecovery()
	engine.Use(recovery)

	router := mux.NewRouter()
	router.PathPrefix(apiPrefix).Handler(createRouter(apiPrefix, server))

	engine.UseHandler(router)
	return engine
}
