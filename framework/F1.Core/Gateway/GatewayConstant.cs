using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.GameActor;

namespace F1.Core.Gateway
{
    public class GatewayConstant
    {
        /// <summary>
        /// Gateway在PD里面也是一个ActorHost, 只是提供的服务名为`GATEWAY`, 进行了特殊处理
        /// </summary>
        public const string ServiceGateway = "GATEWAY";
        /// <summary>
        /// 派发给战斗Player的消息前缀
        /// </summary>
        public const string GamePlayerMessagePrefix = "game.";
        /// <summary>
        /// Gateway将玩家对象分为LogicPlayer和GamePlayer
        /// 其中LogicPlayer是处理一般逻辑的对象, GamePlayer是处理游戏逻辑的对象
        /// 游戏逻辑因为频率较高, 所以一般需要用面向消息的Actor模式来实现
        /// 而普通业务逻辑, 可以通过Virtual Actor来编程
        /// </summary>
        public static readonly string LogicPlayerActorServiceName = typeof(ILogicPlayer).Name;
        public static readonly string GamePlayerActorServiceName = typeof(IGamePlayer).Name;
    }
}
