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
* [x] Socket
    * [x] TcpServer
    * [x] TcpClient
    * [x] Codec
    * [x] ConnectionManager
    * [x] MessageCenter
    * [x] SendingLoop
    * [x] MessageProcessLoop
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
        * [x] Reentrant
    * [x] ActorManager
        * [ ] SlowPath
    * [x] ActorMessageLoop
        * [x] Timer
        * [ ] UserMessage
    * [ ] ActorLifeTime
    * [x] ActorClientProxyFactory
* [ ] Virtual Actor
    * [ ] impl
    * [ ] sample
