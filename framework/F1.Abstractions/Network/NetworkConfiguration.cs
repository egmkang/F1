using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions.Network
{
    public class NetworkConfiguration
    {
        public int SoBackLog { get; set; } = 1024 * 8;
        public int SendWindowSize { get; set; } = 128 * 1024;
        public int RecvWindowSize { get; set; } = 128 * 1024;
        public int ReadTimeout { get; set; } = 15;
        public int WriteTimeout { get; set; } = 15;
        public int WriteBufferHighWaterMark { get; set; } = 1024 * 1024;
        public int WriteBufferLowWaterMark { get; set; } = 32 * 1024;
        public int ConnectTimeout { get; set; } = 5;
        public int EventLoopCount { get; set; } = 2;
    }
}
