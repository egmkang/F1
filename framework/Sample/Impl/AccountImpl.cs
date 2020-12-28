using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using F1.Core.Actor;
using F1.Sample.Interface;
using F1.Abstractions.Network;
using F1.Core.Utils;
using GatewayMessage;
using Sample;
using Sample.Interface;

namespace F1.Sample.Impl
{
    class AccountImpl : Actor, IAccount
    {
        private static SampleCodec codec = new SampleCodec();

        public Task AuthTokenAsync(byte[] token)
        {
            try 
            {
                var playerId = Encoding.UTF8.GetString(token);
                this.Logger.LogInformation("PlayerID:{0}, Token:{1}", this.ID, playerId);
            }
            catch (Exception e) 
            {
                this.Logger.LogError("PlayerID:{0}, AuthTokenAsync, Exception:{1}", this.ID, e);
            }
            return Task.CompletedTask;
        }

        protected override async Task ProcessUserInputMessage(InboundMessage msg)
        {
            //this.Logger.LogInformation("AccountImpl.ProcessUserInputMessage, Type:{1}, Msg:{0}",
            //                            msg.Inner, msg.Inner.GetType().Name);

            var (innerMessage, body) = msg.GetRpcMessage();
            if (innerMessage is NotifyConnectionComing coming)
            {
                await this.ProcessNotifyConnectionComing(coming).ConfigureAwait(false);
            }
            else if (innerMessage is NotifyConnectionAborted aborted)
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

        private async Task ProcessNotifyConnectionComing(NotifyConnectionComing msg) 
        {
            this.Logger.LogInformation("ProcessNotifyConnectionComing, PlayerID:{0}, SessionID:{1}, Token:{2}",
                this.ID, msg.SessionId, msg.Token);
            this.SetSessionID(msg.SessionId);

            var resp = new ResponseLogin();
            resp.Ok = "12121212";

            this.SendMessageToPlayer(resp);
            await Task.CompletedTask;
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

        private static readonly HashSet<string> TestPlayerList = new HashSet<string>()
        {
            "111111", "22222", "33333", "44444", "55555"
        };
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

                if (msg is RequestPlayerList list)
                {
                    var listResponse = new ResponsePlayerList();
                    listResponse.Player.AddRange(TestPlayerList);
                    this.SendMessageToPlayer(listResponse);
                }
                else if (msg is RequestChangePlayer changePlayer)
                {
                    var changePlayerResponse = new ResponseChangePlayer();
                    if (TestPlayerList.Contains(changePlayer.Player))
                    {
                        var player = this.GetActorProxy<IPlayer>(changePlayer.Player);
                        await player.SetAccount(this.ID);

                        changePlayerResponse.Player = changePlayer.Player;

                        var changeDestination = new RequestChangeMessageDestination();
                        changeDestination.SessionId = this.SessionID;
                        changeDestination.NewServiceType = typeof(IPlayer).Name;
                        changeDestination.NewActorId = changePlayer.Player;

                        this.SendMessageToGateway(changeDestination);
                    }
                    else 
                    {
                        changePlayerResponse.Error = $"player not found: {changePlayerResponse.Player}";
                    }
                    this.SendMessageToPlayer(changePlayerResponse);
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

        public void SendMessageToPlayer(IMessage message) 
        {
            var serverId = SessionUniqueSequence.GetServerID(this.SessionID);
            if (serverId != 0)
            {
                var gatewayMessage = new RequestSendMessageToPlayer();
                gatewayMessage.SessionIds.Add(this.SessionID);

                var bytes = codec.EncodeMessage(message);
                gatewayMessage.Msg = ByteString.CopyFrom(bytes);

                if (!this.MessageCenter.SendMessageToServer(serverId, gatewayMessage.ToRpcMessage()))
                {
                    this.Logger.LogWarning("SendMessageToPlayer, PlayerID:{0}, DestServerID:{1}",
                        this.ID, serverId);
                }
            }
            else 
            {
                this.Logger.LogWarning("SendMessageToPlayer, PlayerID:{0}, DestServerID:{1}",
                    this.ID, serverId);
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
    }
}
