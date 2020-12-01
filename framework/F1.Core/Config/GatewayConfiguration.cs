using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.Config
{
    public class GatewayConfiguration
    {
        /// <summary>
        /// PD服务器的地址
        /// </summary>
        public string PlacementDriverAddress { get; set; }
        /// <summary>
        /// 和Host集群通讯的端口, 对外不需要暴露
        /// </summary>
        public int ListenPort { get; set; }
        /// <summary>
        /// 和客户端通讯的端口, 需要对外暴露
        /// </summary>
        public int GatewayPort { get; set; }
        /// <summary>
        /// 消息需要默认派发给哪种Actor服务
        /// 有一个默认值IPlayer
        /// </summary>
        public string ServiceTypeName { get; set; } = "IPlayer";
    }
}
