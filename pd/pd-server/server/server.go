package server

import "go.etcd.io/etcd/embed"

type Server struct {
	//TODO:
	//etcd的成员

	etcd *embed.Etcd
}

func NewServer() *Server {
	s := &Server{}

	return s
}
