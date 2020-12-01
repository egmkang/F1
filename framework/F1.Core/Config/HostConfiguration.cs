using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.Config
{
    public class HostConfiguration
    {
        /// <summary>
        /// PD服务器的地址
        /// </summary>
        public string PlacementDriverAddress { get; set; }
        /// <summary>
        /// Host自己监听的端口
        /// </summary>
        public int ListenPort { get; set; }
    }
}
