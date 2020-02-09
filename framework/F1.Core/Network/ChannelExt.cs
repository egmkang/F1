using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;

namespace F1.Core.Network
{
    public static class ChannelExt
    {
        private const string SessionInfoKey = "SESSIONINFO";

        public static readonly AttributeKey<IConnectionSessionInfo> SESSION_INFO = AttributeKey<IConnectionSessionInfo>.ValueOf(SessionInfoKey);

        public static IConnectionSessionInfo GetSessionInfo(this IChannel channel) 
        {
            return channel.GetAttribute(SESSION_INFO).Get();
        }
    }
}
