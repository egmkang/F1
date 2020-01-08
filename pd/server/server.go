package server

import (
	"github.com/pingcap/log"
	"github.com/pkg/errors"
	"go.etcd.io/etcd/embed"
	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"
	"net/http"
)

type Config struct {
	Name    string
	DataDir string

	ClientUrls          string
	PeerUrls            string
	AdvertiseClientUrls string
	AdvertisePeerUrls   string

	InitialCluster      string
	InitialClusterState string

	LeaderLease int64

	Log log.Config
}

func newConfig() *Config {
	config := &Config{}
	//TODO:
	//从配置文件初始化
	return config
}

type Server struct {
	config     *Config
	etcdConfig *embed.Config
	etcd       *embed.Etcd

	handler http.Handler

	logger      *zap.Logger
	loggerProps *log.ZapProperties
}

func (this *Server) InitLogger() error {
	logger, loggerProps, err := log.InitLogger(&this.config.Log, zap.AddStacktrace(zapcore.DebugLevel))
	if err != nil {
		return err
	}
	this.logger = logger
	this.loggerProps = loggerProps
	return nil
}

func (this *Server) InitEtcd(path string, apiRegister func(*Server) http.Handler) error {
	this.etcdConfig = embed.NewConfig()
	this.etcdConfig.Dir = "default.etcd"
	if path[len(path)-1] != '/' {
		path = path + "/"
	}
	this.etcdConfig.UserHandlers = map[string]http.Handler{path: apiRegister(this)}
	this.etcdConfig.ZapLoggerBuilder = embed.NewZapCoreLoggerBuilder(this.logger, this.logger.Core(), this.loggerProps.Syncer)
	this.etcdConfig.Logger = "zap"

	etcd, err := embed.StartEtcd(this.etcdConfig)
	if err != nil {
		return errors.WithStack(err)
	}

	select {
	case <-etcd.Server.ReadyNotify():
		println("etcd inited")
	}

	this.etcd = etcd

	return nil
}

func NewServer() *Server {
	config := newConfig()
	s := &Server{config: config}
	return s
}
