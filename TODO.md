F1 Framework:

v0.5
* [ ] tracing

v0.4
* [ ] PlacementDriver
  * [ ] Consul Raft / DragonBoat
  * [ ] Virtual Nodes Management
  * [ ] Object Location
* [ ] Load Balance

v0.3
* [ ] RPC重新实现
  * [ ] RPC Meta
  * [ ] RPC Protocol
  * [ ] lz4
* [ ] Protobuf Codec (Span Support)

v0.2
* [x] PlacementDriver
    * [x] Interface and Impl Map
    * [x] configuration(from TiDB's PD)
* [x] Utils
    * [x] SequenceID
    * [x] SessionUniqueID
* [ ] Configuration
  * [ ] network
  * [ ] actor
  * [x] host
  * [x] gateway
* [ ] Abstraction
  * [ ] Gateway
    * [ ] Authentication
    * [ ] Message Codec
    * [ ] Message Processing
  * [ ] Actor
    * [x] Interface And Impl Map
    * [ ] GC
    * [ ] Actor LifeTime Configuration
* [ ] Sample
    * [ ] LogicServer
    * [ ] GameServer
    * [ ] MultiPlayer(One OpenID)

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
* [ ] Actor
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
    * [ ] Actor Model
* [x] Gateway
    * [x] Gateway Client
    * [x] Message Process
    * [x] Gateway Service
* [x] GameServer
  * [x] DispatchUserMessage
