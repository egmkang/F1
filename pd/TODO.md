PlacementDriver:

* [ ] 名字空间
* [x] ID生成
    * [x] ServerID Generator
    * [x] Sequence Generator
* [ ] PD节点
    * [ ] 心跳
* [x] 节点
    * [x] 注册服务器
    * [x] 续约服务器
    * [x] 服务器索引
    * [x] 拉取最近的事件(新老节点状态)
* [ ] Actor
    * [ ] 定位(包含ActorID, ServerUniqueID, ActorVersion)
    * [ ] 修改位置(老Version信息过期)
    * [ ] 存档读写所有权(考虑用Version来做)
    * [ ] 一次性定位(不会转移, 死了就死了)
* [ ] MultiRaft
    * [ ] 集群节点事件
    * [ ] 负载均衡以及调度
* [ ] 心跳续约服务器存活时间
* [ ] gossip广播(及时更新其他pd node状态)
* [ ] 集群脑裂处理
