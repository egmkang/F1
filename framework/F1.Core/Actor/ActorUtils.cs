using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using F1.Abstractions.Network;
using F1.Abstractions.RPC;
using F1.Core.RPC;
using Google.Protobuf;
using RpcMessage;

namespace F1.Core.Actor
{
    internal sealed class ActorUtils
    {
        static Func<byte[], ByteString> CreateByteStringByBytes = null;

        static ActorUtils() 
        {
            //减少一次拷贝
            if (CreateByteStringByBytes == null) 
            {
                var param = Expression.Parameter(typeof(byte[]), "bytes");
                var ctor = typeof(ByteString).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(byte[]) }, null);
                var lambda = Expression.Lambda<Func<byte[], ByteString>>(
                    Expression.New(ctor, param), param);
                CreateByteStringByBytes = lambda.Compile();
            }
        }

        public static void SendRepsonseRpcError(InboundMessage inboundMessage, IMessageCenter messageCenter, RpcErrorCode errorCode, string errorMessage)
        {
            var request = inboundMessage.Inner as RequestRpc;

            var response = new ResponseRpc();
            response.Request = request;
            response.RequestId = request.RequestId;
            response.ResponseId = request.ResponseId;
            response.ErrorCode = (int)errorCode;
            response.ErrorMsg = errorMessage;

            var outboundMessage = new OutboundMessage(inboundMessage.SourceConnection, response);
            messageCenter.SendMessage(outboundMessage);
        }

        public static void SendResponseRpc(InboundMessage inboundMessage, IMessageCenter messageCenter, object returnValue, IParametersSerializer serializer)
        {
            var request = inboundMessage.Inner as RequestRpc;

            var response = new ResponseRpc();
            response.RequestId = request.RequestId;
            response.ResponseId = request.ResponseId;
            response.Response = CreateByteStringByBytes(serializer.Serialize(returnValue, typeof(object)));

            var outboundMessage = new OutboundMessage(inboundMessage.SourceConnection, response);
            messageCenter.SendMessage(outboundMessage);
        }
    }
}
