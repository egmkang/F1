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
}
