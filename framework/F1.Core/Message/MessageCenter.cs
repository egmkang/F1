﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using F1.Abstractions.Network;
using F1.Core.Network;
using Microsoft.Extensions.Logging;

namespace F1.Core.Message
{
    public sealed class MessageCenter : IMessageCenter
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;
        private readonly IConnectionManager connectionManager;
        private readonly InboundMessageQueue inboundMessageQueue;

        private int inboundMessageQueueSize = 0;
        private Action<IInboundMessage> inboundMessageProc;
        private Action<IChannel> channelClosedProc;
        private Action<IOutboundMessage> failMessageProc;

        private bool stop;

        public MessageCenter(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IConnectionManager connectionManager) 
        {
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            this.connectionManager = connectionManager;
            this.logger = this.loggerFactory.CreateLogger("F1.MessageCenter");

            this.inboundMessageQueue = new InboundMessageQueue(loggerFactory);
        }

        public void RegsiterEvent(Action<IInboundMessage> inboundMessageProc,
            Action<IChannel> channelClosedProc,
            Action<IOutboundMessage> failMessageProc) 
        {
            this.inboundMessageProc = inboundMessageProc;
            this.channelClosedProc = channelClosedProc;
            this.failMessageProc = failMessageProc;
        }

        public void StartAsync()
        {
            var reader = this.inboundMessageQueue.Reader;
            Task.Run(async () =>
            {
                while (this.stop) 
                {
                    try 
                    {
                        var inboundMessage = await reader.ReadAsync();
                        if (inboundMessage == null)
                            break;
                        Interlocked.Decrement(ref this.inboundMessageQueueSize);
                        this.inboundMessageProc(inboundMessage);
                    }
                    catch (Exception e) 
                    {
                        this.logger.LogError("MessageCenter Process InboundMessage, Exception:{0}, StackTrace:{1}", e, e.StackTrace.ToString());
                    }
                }
            });
        }

        public void StopAsync()
        {
            this.stop = true;
            this.inboundMessageQueue.ShutDown();
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

        public void OnMessageFail(IOutboundMessage message)
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

        public void OnReceivedMessage(IInboundMessage message)
        {
            this.inboundMessageQueue.PushMessage(message);
            if (Interlocked.Increment(ref this.inboundMessageQueueSize) > 10000) 
            {
                this.logger.LogWarning("InboundMessage Queue Size:{0}", this.inboundMessageQueueSize);
            }
        }

        public void SendMessage(IOutboundMessage message)
        {
            var sessionInfo = message.DestConnection.GetSessionInfo();
            if (sessionInfo.PutOutboundMessage(message) > 1000)
            {
                this.logger.LogWarning("SessionID:{0}, SendingQueueCount:{1}",
                   sessionInfo.SessionID, sessionInfo.SendingQueueCount);
            }
        }
    }
}