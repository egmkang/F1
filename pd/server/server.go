package server

import (
	"github.com/pingcap/log"
	"github.com/pkg/errors"
	"go.etcd.io/etcd/clientv3"
	"go.etcd.io/etcd/embed"
	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"
	"net/http"
	"sync"
	"time"
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
	etcdClient *clientv3.Client

	handler http.Handler

	logger      *zap.Logger
	loggerProps *log.ZapProperties

	hostMutex sync.Mutex
	hosts     map[int64]*ActorHostInfo
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
		log.Error("StartEtcd failed", zap.Error(err))
		return errors.WithStack(err)
	}

	select {
	case <-etcd.Server.ReadyNotify():
		log.Info("etcd inited")
	}

	endpoints := []string{this.etcdConfig.ACUrls[0].String()}
	log.Info("create etcd v3 client", zap.Strings("endpoints", endpoints))

	client, err := clientv3.New(clientv3.Config{
		Endpoints:   endpoints,
		DialTimeout: 3 * time.Second,
	})

	if err != nil {
		return errors.WithStack(err)
	}

	this.etcd = etcd
	this.etcdClient = client
	return nil
}

func (this *Server) GetEtcdClient() *clientv3.Client {
	return this.etcdClient
}

func NewServer() *Server {
	config := newConfig()
	s := &Server{
		config:    config,
		hostMutex: sync.Mutex{},
		hosts:     map[int64]*ActorHostInfo{}}
	return s
}
