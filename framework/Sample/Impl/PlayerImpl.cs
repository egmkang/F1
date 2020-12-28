using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using F1.Abstractions.Network;
using F1.Core.Actor;
using F1.Core.Utils;
using F1.Sample.Interface;
using F1.Sample.Impl;
using GatewayMessage;
using Sample.Interface;

namespace Sample.Impl
{
    class PlayerImpl : Actor, IPlayer
    {
        private static SampleCodec codec = new SampleCodec();

        public Task<string> EchoAsync(string name)
        {
            return Task.FromResult(name);
        }

        protected override async Task ProcessUserInputMessage(InboundMessage msg)
        {
            //this.Logger.LogInformation("PlayerImpl.ProcessUserInputMessage, Type:{1}, Msg:{0}",
            //                            msg.Inner, msg.Inner.GetType().Name);

            var (innerMessage, body) = msg.GetRpcMessage();
            if (innerMessage is NotifyConnectionAborted aborted)
            {
                await this.ProcessNotifyConnectionAborted(aborted).ConfigureAwait(false);
            }
            else if (innerMessage is NotifyNewMessage newMessage)
            {
                await this.ProcessNotifyNewMessage(msg.SourceConnection, newMessage, body).ConfigureAwait(false);
            }
            else
            {
                this.Logger.LogWarning("ProdessUserInputMessage, PlayerID:{0} MsgTye:{1} not process",
                    this.ID, msg.Inner.GetType());
            }
        }

        private async Task ProcessNotifyConnectionAborted(NotifyConnectionAborted msg)
        {
            this.Logger.LogInformation("ProcessNotifyConnectionAborted, PlayerID:{0}, SessionID:{1}",
                this.ID, msg.SessionId);
            if (msg.SessionId == this.SessionID)
            {
                this.SetSessionID(0);
            }
            await Task.CompletedTask;
        }

        private async Task ProcessNotifyNewMessage(IChannel channel, NotifyNewMessage newMessage, byte[] body)
        {
            this.BeforeProcessUserMessage();
            try
            {
                if (this.SessionID != newMessage.SessionId)
                {
                    this.SetSessionID(newMessage.SessionId);
                }

                var msg = codec.DecodeMessage(body);
                //this.Logger.LogInformation("ProcessNotifyNewMessage, MsgType:{0}, Content:{1}", msg.GetType(), msg);

                if (msg is RequestEcho hello)
                {
                    var content = await this.EchoAsync(hello.Content).ConfigureAwait(false);
                    var resp = new ResponseEcho()
                    {
                        Content = content,
                    };

                    this.SendMessageToPlayer(resp);
                    //this.SendMessageToPlayer(resp, $"echo response:{content}");
                }
                else if (msg is RequestGoBack goback)
                {
                    var backupResponse = new ResponseGoBack();
                    this.SendMessageToPlayer(backupResponse);

                    var changeMessageDestination = new RequestChangeMessageDestination();
                    changeMessageDestination.SessionId = this.SessionID;
                    changeMessageDestination.NewServiceType = typeof(IAccount).Name;
                    changeMessageDestination.NewActorId = this.Account;

                    this.SendMessageToGateway(changeMessageDestination);
                }
                else if (msg is RequestGetID getId)
                {
                    var getIdResponse = new ResponseGetID();
                    getIdResponse.ActorType = this.ActorType.Name;
                    getIdResponse.ActorId = this.ID;

                    this.SendMessageToPlayer(getIdResponse);
                }
                else
                {
                    this.Logger.LogInformation("ProcessNotifyNewMessage, PlayerID:{0}, Type:{2}, Msg:{1}",
                        this.ID, msg, msg.GetType().Name);
                }
            }
            catch (Exception e)
            {
                this.Logger.LogError("ProcessNotifyNewMessage, PlayerID:{0}, Exception:{1}", this.ID, e);
            }
            this.EndProcessUserMessage();
        }

        public void SendMessageToPlayer(IChannel channel, IMessage message)
        {
            var gatewayMessage = new RequestSendMessageToPlayer();
            gatewayMessage.SessionIds.Add(this.SessionID);

            var bytes = codec.EncodeMessage(message);
            gatewayMessage.Msg = ByteString.CopyFrom(bytes);

            this.MessageCenter.SendMessage(new OutboundMessage(channel, gatewayMessage));
        }

        public void SendMessageToPlayer(IMessage message, string trace = "")
        {
            var serverId = SessionUniqueSequence.GetServerID(this.SessionID);
            if (serverId != 0)
            {
                var gatewayMessage = new RequestSendMessageToPlayer();
                gatewayMessage.Trace = trace;
                gatewayMessage.SessionIds.Add(this.SessionID);

                var bytes = codec.EncodeMessage(message);
                gatewayMessage.Msg = ByteString.CopyFrom(bytes);

                if (!this.MessageCenter.SendMessageToServer(serverId, gatewayMessage.ToRpcMessage()))
                {
                    this.Logger.LogWarning("SendMessageToPlayer, Actor:{0}/{1}, DestServerID:{2}",
                        this.ActorType, this.ID, serverId);
                }
            }
            else
            {
                this.Logger.LogWarning("SendMessageToPlayer, Actor:{0}/{1}, DestServerID:{2}",
                    this.ActorType, this.ID, serverId);
            }
        }

        public void SendMessageToGateway(IMessage message) 
        {
            var serverId = SessionUniqueSequence.GetServerID(this.SessionID);
            if (serverId != 0)
            {
                if (!this.MessageCenter.SendMessageToServer(serverId, message.ToRpcMessage()))
                {
                    this.Logger.LogWarning("SendMessageToGateway, Actor:{0}/{1}, DestServerID:{2}",
                       this.ActorType, this.ID, serverId);
                }
            }
            else
            {
                this.Logger.LogWarning("SendMessageToGateway, Actor:{0}/{1}, DestServerID:{2}",
                    this.ActorType, this.ID, serverId);
            }
        }

 
        public string Account { get; set; }
        public Task SetAccount(string account)
        {
            this.Account = account;
            return Task.CompletedTask;
        }
    }
}
