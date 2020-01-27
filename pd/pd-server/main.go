package main

import (
	"github.com/pingcap/log"
	"pd/server"
	"pd/server/api"
	"time"
)

func main() {
	server := server.NewServer()
	server.InitLogger()

	server.InitEtcd(api.ApiPrefix, api.NewHandle)

	for server.IsRunning() {
		time.Sleep(time.Second)
	}
	log.Info("exit")
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
