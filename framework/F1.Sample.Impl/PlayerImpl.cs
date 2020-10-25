﻿using System;
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

namespace F1.Sample.Impl
{
    class PlayerImpl : Actor, IPlayer
    {
        private static SampleCodec codec = new SampleCodec();

        private long currentSessionID = 0;

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

        public Task<string> EchoAsync(string name)
        {
            //this.Logger.LogInformation("PlayerID:{0} SayHelloAsync, Name:{1}", this.ID, name);
            return Task.FromResult(name);
        }

        private void SetSessionID(long newSessionID) 
        {
            if (this.currentSessionID != newSessionID) 
            {
                this.Logger.LogInformation("PlayerID:{0}, SessionID:{1} => NewSessionID:{2}",
                    this.ID, this.currentSessionID, newSessionID);
                this.currentSessionID = newSessionID;
            }
        }

        protected override async Task ProcessUserInputMessage(InboundMessage msg)
        {
            if (msg.Inner is NotifyConnectionComing)
            {
                await this.ProcessNotifyConnectionComing(msg.Inner as NotifyConnectionComing).ConfigureAwait(false);
            }
            else if (msg.Inner is NotifyConnectionAborted)
            {
                await this.ProcessNotifyConnectionAborted(msg.Inner as NotifyConnectionAborted).ConfigureAwait(false);
            }
            else if (msg.Inner is NotifyNewMessage)
            {
                await this.ProcessNotifyNewMessage(msg.SourceConnection, msg.Inner as NotifyNewMessage).ConfigureAwait(false);
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
            if (msg.SessionId == this.currentSessionID) 
            {
                this.SetSessionID(0);
            }
            await Task.CompletedTask;
        }

        public void SendMessageToPlayer(IChannel channel, IMessage message)
        {
            var gatewayMessage = new RequestSendMessageToPlayer();
            gatewayMessage.SessionIds.Add(this.currentSessionID);

            var bytes = codec.EncodeMessage(message);
            gatewayMessage.Msg = ByteString.CopyFrom(bytes);

            this.MessageCenter.SendMessage(new OutboundMessage(channel, gatewayMessage));
        }

        public void SendMessageToPlayer(IMessage message) 
        {
            var serverId = SessionUniqueSequence.GetServerID(this.currentSessionID);
            if (serverId != 0)
            {
                var gatewayMessage = new RequestSendMessageToPlayer();
                gatewayMessage.SessionIds.Add(this.currentSessionID);

                var bytes = codec.EncodeMessage(message);
                gatewayMessage.Msg = ByteString.CopyFrom(bytes);

                if (!this.MessageCenter.SendMessageToServer(serverId, gatewayMessage))
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

        private async Task ProcessNotifyNewMessage(IChannel channel, NotifyNewMessage newMessage) 
        {
            try 
            {
                var msg = codec.DecodeMessage(newMessage.Msg.ToByteArray());
                //this.Logger.LogInformation("ProcessNotifyNewMessage, MsgType:{0}, Content:{1}", msg.GetType(), msg);

                if (msg is RequestEcho hello) 
                {
                    var content = await this.EchoAsync(hello.Content).ConfigureAwait(false);
                    var resp = new ResponseEcho()
                    {
                        Content = content,
                    };

                    this.SendMessageToPlayer(resp);
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
        }
    }
}
