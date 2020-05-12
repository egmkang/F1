C# Framework:

* [ ] Placement SDK
    * [x] RegisterServer
    * [x] KeepAliveServer
    * [x] FindActorPositon
    * [x] ids/tokens
    * [x] ServerEvents:
        * [x] Add
        * [x] Remove
        * [ ] Offline(LRU¸ÄÔì)
* [ ] Socket
    * [x] TcpServer
    * [x] TcpClient
    * [x] Codec
    * [x] ConnectionManager
    * [x] MessageCenter
    * [x] SendingLoop
    * [ ] MessageProcessLoop
* [ ] RPC
    * [x] ClientManager
        * [ ] Client Reconnect
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
    * [x] ActorManager
        * [ ] SlowPath
    * [x] ActorMessageLoop
        * [ ] Timer
        * [ ] UserMessage
    * [ ] ActorLifeTime
    * [x] ActorClientProxyFactory
* [ ] Virtual Actor
    * [ ] impl
    * [ ] sample
