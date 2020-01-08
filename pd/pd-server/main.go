package main

import (
	"pd/pd-server/api"
	"pd/pd-server/server"
	"time"
)

func main() {
	server := server.NewServer()
	server.InitLogger()

	server.InitEtcd(api.ApiPrefix, api.NewHandle)

	for {
		time.Sleep(time.Second)
	}
	//cfg := embed.NewConfig()
	//cfg.Dir = "default.etcd"
	//etcd, err := embed.StartEtcd(cfg)
	//if err != nil {
	//	log.Fatal(err)
	//}
	//defer etcd.Close()
	//select {
	//case <-etcd.Server.ReadyNotify():
	//	log.Printf("Server is ready!")
	//case <-time.After(60 * time.Second):
	//	etcd.Server.Stop() // trigger a shutdown
	//	log.Printf("Server took too long to start!")
	//}
	//log.Fatal(<-etcd.Err())
}
