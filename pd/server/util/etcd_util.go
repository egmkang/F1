package util

import (
	"context"
	"github.com/pingcap/log"
	"github.com/pkg/errors"
	"go.etcd.io/etcd/clientv3"
	"go.uber.org/zap"
	"time"
)

const DefaultRequestTimeout = time.Second * 10
const DefaultSlowRequest = time.Second * 1

func EtcdKVGet(client *clientv3.Client, key string, opts ...clientv3.OpOption) (*clientv3.GetResponse, error) {
	ctx, cancel := context.WithTimeout(client.Ctx(), DefaultRequestTimeout)
	defer cancel()

	start := time.Now()
	resp, err := clientv3.NewKV(client).Get(ctx, key, opts...)
	if err != nil {
		log.Error("EtcdKVGet error", zap.Error(err))
	}
	if cost := time.Since(start); cost > DefaultSlowRequest {
		log.Warn("EtcdKVGet get too slow", zap.String("key", key), zap.Duration("cost", cost), zap.Error(err))
	}
	return resp, errors.WithStack(err)
}

func getKV(client *clientv3.Client, key string, opts ...clientv3.OpOption) (*clientv3.GetResponse, error) {
	resp, err := EtcdKVGet(client, key, opts...)
	if err != nil {
		return nil, err
	}
	if n := len(resp.Kvs); n == 0 {
		return nil, nil
	} else if n > 1 {
		return nil, errors.Errorf("invalid get value resp %v", resp.Kvs)
	}
	return resp, nil
}

func getKVValue(client *clientv3.Client, key string, opts ...clientv3.OpOption) ([]byte, error) {
	resp, err := getKV(client, key, opts...)
	if err != nil {
		return nil, err
	}
	if resp == nil {
		return nil, nil
	}
	return resp.Kvs[0].Value, nil
}

type slowLogTxn struct {
	clientv3.Txn
	cancel context.CancelFunc
}

func Txn(client *clientv3.Client) clientv3.Txn {
	ctx, cancel := context.WithTimeout(client.Ctx(), DefaultRequestTimeout)
	return &slowLogTxn{
		Txn:    client.Txn(ctx),
		cancel: cancel,
	}
}

func (this *slowLogTxn) If(cs ...clientv3.Cmp) clientv3.Txn {
	return &slowLogTxn{
		Txn:    this.Txn.If(cs...),
		cancel: this.cancel,
	}
}

func (this *slowLogTxn) Then(op ...clientv3.Op) clientv3.Txn {
	return &slowLogTxn{
		Txn:    this.Txn.Then(op...),
		cancel: this.cancel,
	}
}

func (this *slowLogTxn) Commit() (*clientv3.TxnResponse, error) {
	start := time.Now()
	defer this.cancel()
	resp, err := this.Txn.Commit()

	cost := time.Since(start)
	if cost > DefaultSlowRequest {
		log.Warn("txn too slow", zap.Error(err), zap.Reflect("resp", resp), zap.Duration("cost", cost))
	}

	//TODO:
	//metrics

	return resp, errors.WithStack(err)
}
