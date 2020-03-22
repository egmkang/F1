﻿using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.Network;

namespace F1.Abstractions.Actor
{
    public interface IActorContext
    {
        /// <summary>
        /// 获取当前正在做的请求
        /// 如果是Actor调用其他Actor, 那么Context是需要有传染性
        /// 如果是Client调用ActorProxy, 那么不需要传染
        /// </summary>
        (long ServerID, long RequestID) CurrentRequest { get; }
        /// <summary>
        /// 获取当前的LoopID
        /// </summary>
        long RunningLoopID { get; }
        /// <summary>
        /// 给Actor发送消息, 等待Loop处理
        /// </summary>
        /// <param name="inboundMessage">需要被处理的消息</param>
        void SendMessage(InboundMessage inboundMessage);
    }
}
