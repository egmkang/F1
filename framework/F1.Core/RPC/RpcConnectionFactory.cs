using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Network;
using F1.Core.Message;
using F1.Message;

namespace F1.Core.RPC
{
    internal class RpcConnectionFactory
    {
        private WeakReference<IChannel> channel;
        private IMessageCenter messageCenter;

        public RpcConnectionFactory(ILoggerFactory loggerFactory, IMessageCenter messageCenter) 
        {
            this.messageCenter = messageCenter;
        }

        

        public Task SendRpcRequest(RequestRpc request)
        {
            return Task.Delay(0);
        }
    }
}
