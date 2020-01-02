PlacementDriver:

* [ ] 名字空间
* [ ] ID生成(服务器ID, Version)
* [ ] 节点
    * [ ] 注册服务器
    * [ ] 续约服务器
    * [ ] 拉取最近的事件(新老节点状态, Actor信息)
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
