﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Core.Network;
using F1.Core.Utils;

namespace F1.Core.Message
{
    public sealed class MessageCenter : IMessageCenter
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;
        private readonly IConnectionManager connectionManager;
        private readonly AsyncMessageQueue<InboundMessage> inboundMessageQueue;
        private readonly Dictionary<string, Action<InboundMessage>> inboudMessageProc = new Dictionary<string, Action<InboundMessage>>();

        private Action<IChannel> channelClosedProc;
        private Action<OutboundMessage> failMessageProc;
        private Action<InboundMessage> defaultInboundMessageProc;

        public MessageCenter(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IConnectionManager connectionManager) 
        {
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            this.connectionManager = connectionManager;
            this.logger = this.loggerFactory.CreateLogger("F1.MessageCenter");

            this.inboundMessageQueue = new AsyncMessageQueue<InboundMessage>();

            this.StartAsync();
        }

        public void RegsiterEvent(Action<IChannel> channelClosedProc,
            Action<OutboundMessage> failMessageProc) 
        {
            this.channelClosedProc = channelClosedProc;
            this.failMessageProc = failMessageProc;
        }

        public void StartAsync()
        {
            var reader = this.inboundMessageQueue.Reader;
            Task.Run(async () =>
            {
                while (this.inboundMessageQueue.Valid) 
                {
                    var more = await reader.WaitToReadAsync();
                    if (!more) 
                    {
                        break;
                    }

                    InboundMessage message = default;
                    while (reader.TryRead(out message) && message.Inner != null)
                    {
                        this.inboundMessageQueue.QueueCount--;
                        try
                        {
                            this.ProcessInboundMessage(message);
                        }
                        catch (Exception e)
                        {
                            this.logger.LogError("MessageCenter Process InboundMessage, Exception:{0}, StackTrace:{1}", e, e.StackTrace.ToString());
                        }
                    }
                }
                this.logger.LogInformation("MessageCenter Exit");
            });
        }

        public void StopAsync()
        {
            this.inboundMessageQueue.ShutDown(InboundMessage.Empty);
        }

        public void OnConnectionClosed(IChannel channel)
        {
            var sessionInfo = channel.GetSessionInfo();
            this.connectionManager.RemoveConnection(channel);
            try
            {
                this.channelClosedProc(channel);
            }
            catch (Exception e)
            {
                this.logger.LogError("MessageCenter OnConnectionClosed, ChannelID:{0}, Exception:{1}, StackTrace:{2}",
                    channel.GetSessionInfo().SessionID, e, e.StackTrace.ToString());
            }
        }

        public void OnMessageFail(OutboundMessage message)
        {
            try
            {
                this.failMessageProc(message);
            }
            catch (Exception e)
            {
                this.logger.LogError("MessageCenter OnMessageFail, Exception:{0}, StackTrace:{1}", e, e.StackTrace.ToString());
            }
        }

        public void OnReceivedMessage(InboundMessage message)
        {
            if (this.logger.IsEnabled(LogLevel.Trace)) 
            {
                var sessionInfo = message.SourceConnection.GetSessionInfo();
                this.logger.LogTrace("MessageCenter.OnRecievedMessage, SessionID:{0}, MilliSeconds:{1}, Data:{2}",
                    sessionInfo.SessionID, message.MilliSeconds, message.Inner.ToString());
            }

            this.inboundMessageQueue.PushMessage(message);
            if (this.inboundMessageQueue.QueueCount > 10000)
            {
                this.logger.LogWarning("InboundMessage Queue Size:{0}", this.inboundMessageQueue.QueueCount);
            }
        }

        public void SendMessage(OutboundMessage message)
        {
            var sessionInfo = message.DestConnection.GetSessionInfo();
            var size = 0;
            if ((size = sessionInfo.PutOutboundMessage(message)) > 1000)
            {
                this.logger.LogWarning("SessionID:{0}, SendingQueueCount:{1}",
                   sessionInfo.SessionID, size);
            }
        }

        public void RegisterMessageProc(string messageName, Action<InboundMessage> action)
        {
            if (string.IsNullOrEmpty(messageName)) 
            {
                this.defaultInboundMessageProc = action;
                this.logger.LogInformation("RegisterMessageProc default proc");
                return;
            }
            if (!this.inboudMessageProc.TryAdd(messageName, action))
            {
                this.logger.LogError("RegisterMessageProc, MessageName:{0} exists", messageName);
            }
        }

        private void ProcessInboundMessage(InboundMessage message) 
        {
            if (this.defaultInboundMessageProc == null) 
            {
                this.logger.LogError("ProcessInboundMessage but DefaultInboundMessageProc is null");
                return;
            }
            if (string.IsNullOrEmpty(message.MessageName)) 
            {
                this.defaultInboundMessageProc(message);
                return;
            }
            if (this.logger.IsEnabled(LogLevel.Debug)) 
            {
                if (message.MessageName != "RpcMessage.RequestRpcHeartBeat" &&
                    message.MessageName != "RpcMessage.ResponseRpcHeartBeat") 
                {
                    this.logger.LogDebug("ProcessMessage, MessageName:{0}", message.MessageName);
                }
            }
            if (this.inboudMessageProc.TryGetValue(message.MessageName, out var proc))
            {
                proc(message);
            }
            else 
            {
                this.defaultInboundMessageProc(message);
            }
        }
    }
}
