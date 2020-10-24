package main

import (
	"github.com/pingcap/log"
	"os"
	"pd/server"
	"pd/server/api"
	"pd/server/config"
	"time"
)

func main() {
	cfg := config.NewConfig()
	err := cfg.Parse(os.Args[1:])

	if err != nil {
		os.Exit(0)
	}

	err = cfg.SetupLogger()

	server := server.NewServer(cfg)
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
