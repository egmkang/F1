package main

import (
	"log"
	"net/http"
	"time"

	"go.etcd.io/etcd/embed"
)

func main() {
	http.HandleFunc("/api/register", func(w http.ResponseWriter, r *http.Request) {
	})

	go func() {
		http.ListenAndServe(":80", nil)
	}()

	cfg := embed.NewConfig()
	cfg.Dir = "default.etcd"
	e, err := embed.StartEtcd(cfg)
	if err != nil {
		log.Fatal(err)
	}
	defer e.Close()
	select {
	case <-e.Server.ReadyNotify():
		log.Printf("Server is ready!")
	case <-time.After(60 * time.Second):
		e.Server.Stop() // trigger a shutdown
		log.Printf("Server took too long to start!")
	}
	log.Fatal(<-e.Err())
}
