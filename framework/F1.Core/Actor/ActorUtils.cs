using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Abstractions.RPC;
using F1.Core.Message;
using F1.Core.RPC;
using Google.Protobuf;
using Rpc;


namespace F1.Core.Actor
{
    internal sealed class ActorUtils
    {
        public static void SendRepsonseRpcError(IChannel sourceChannel, RpcRequest req, IMessageCenter messageCenter, RpcErrorCode errorCode, string errorMessage)
        {
            var resp = new RpcMessage()
            {
                Meta = new RpcResponse()
                {
                    RequestId = req.RequestId,
                    CallId = req.CallId,
                    ErrorCode = (int)errorCode,
                    ErrorText = errorMessage,
                },
            };

            var outboundMessage = new OutboundMessage(sourceChannel, resp);
            messageCenter.SendMessage(outboundMessage);
        }

        public static void SendResponseRpc(IChannel sourceChannel, RpcRequest req, IMessageCenter messageCenter, object returnValue, IParametersSerializer serializer)
        {
            var body = serializer.Serialize(returnValue, typeof(object));
            var resp = new RpcMessage() 
            {
                Meta = new RpcResponse()
                {
                    RequestId = req.RequestId,
                    CallId = req.CallId,
                    EncodingType = req.EncodingType,
                },
                //TODO:
                //LZ4
                Body = body,
            };

            var outboundMessage = new OutboundMessage(sourceChannel, resp);
            messageCenter.SendMessage(outboundMessage);
        }
    }
}
