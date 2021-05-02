F1 Framework:


v0.5
* [ ] tracing
  * [ ] rpc request sequence
* [ ] Reminder
* [ ] Actor LifeTime Configuration
* [ ] More Sample
* [ ] Actor System Support

v0.4
* [x] PlacementDriver
  * [x] API重构
  * [x] 提供一个Placement的存储层抽象, 目前使用redis来当存储实现
  * [ ] C#部分还没跟着重构

v0.3
* [x] RPC重新实现
  * [x] RPC Meta
  * [x] RPC Protocol
    * [x] codec
    * [x] dispatch
  * [x] args encoding (ceras/msgpack)
* [x] Protobuf Codec (Span Support)
* [x] Gateway NewMessageComing
* [x] Gateway SendMessageToPlayer

v0.2
* [x] PlacementDriver
    * [x] Interface and Impl Map
    * [x] configuration(from TiDB's PD)
* [x] Utils
    * [x] SequenceID
    * [x] SessionUniqueID
* [x] Configuration
  * [x] network
  * [x] host
  * [x] gateway
* [x] Actor
  * [x] Interface And Impl Map
* [x] Sample
    * [x] MultiPlayer(One OpenID)

v0.1

* [x] PlacementDriver
    * [x] ID Generator
      * [x] ServerID Generator
      * [x] Sequence Generatro
    * [x] Virtual Actor Host Node
      * [x] register server instance
      * [x] keep alive server
      * [x] actor service index
      * [x] pull recent event
    * [x] Actor
      * [x] Placement
      * [x] OneTime Actor(never move to another actor host)
* [x] Placement SDK
    * [x] RegisterServer
    * [x] KeepAliveServer
    * [x] FindActorPositon
    * [x] ids/tokens
    * [x] ServerEvents:
        * [x] Add
        * [x] Remove
        * [x] Offline
    * [x] Remove Domain
    * [x] Gateway
* [x] Socket
    * [x] TcpServer
    * [x] TcpClient
    * [x] Codec
        * [x] ProtobufCodec
        * [x] BlockMessageCodec
    * [x] ConnectionManager
    * [x] MessageCenter
    * [x] SendingLoop
    * [x] MessageProcessLoop
    * [x] Auto Reconnect Client Factory
        * [x] Add/Remove
        * [x] HeartBeat
* [x] RPC
    * [x] ClientManager
        * [x] Client Add/Remove
        * [x] HeartBeat
    * [x] Future/Promise
    * [x] ClientProxy
    * [x] ServerInvoker
    * [x] ObjectSerializer
* [x] Actor
    * [x] ServerContext
    * [x] Actor
    * [x] ActorContext
        * [x] Reentrant
    * [x] ActorManager
        * [x] SlowPath
    * [x] ActorMessageLoop
        * [x] Timer
        * [x] UserMessage(not tested)
    * [x] ActorLifeTime
    * [x] ActorClientProxyFactory
    * [x] Multi Interface
* [x] Gateway
    * [x] Gateway Client
    * [x] Message Process
    * [x] Gateway Service
* [x] GameServer
  * [x] DispatchUserMessage
