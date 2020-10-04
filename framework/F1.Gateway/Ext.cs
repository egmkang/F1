using F1.Abstractions.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Gateway
{

    public class GatewayPlayerSessionInfo 
    {
        public long PlayerID;
        public string OpenID;
        public byte[] Token;
    }
    public static partial class Ext
    {
        private const string KeyGatewaySessionInfo = "KeyGatewaySessionInfo";

        public static GatewayPlayerSessionInfo GetPlayerInfo(this IConnectionSessionInfo sessionInfo) 
        {
            if (sessionInfo.States.TryGetValue(KeyGatewaySessionInfo, out var v)) 
            {
                return v as GatewayPlayerSessionInfo;
            }
            var info = new GatewayPlayerSessionInfo();
            sessionInfo.States.TryAdd(KeyGatewaySessionInfo, info);
            return info;
        }
    }
}
