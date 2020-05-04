using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.Network;
using F1.Abstractions.RPC;
using RpcMessage;

namespace F1.Core.Actor
{
    internal sealed class ActorUtils
    {
        public static void SendRepsonseRpcError(InboundMessage inboundMessage, IMessageCenter messageCenter, int errorCode, string errorMessage)
        {
            var request = inboundMessage.Inner as RequestRpc;

            var response = new ResponseRpc();
            response.Request = request;
            response.RequestId = request.RequestId;
            response.ResponseId = request.ResponseId;
            response.ErrorCode = errorCode;
            response.ErrorMsg = errorMessage;

            var outboundMessage = new OutboundMessage(inboundMessage.SourceConnection, response);
            messageCenter.SendMessage(outboundMessage);
        }

        public static void SendResponseRpc(InboundMessage inboundMessage, IMessageCenter messageCenter, object returnValue, IParametersSerializer serializer)
        {
            //TODO
        }
    }
}
