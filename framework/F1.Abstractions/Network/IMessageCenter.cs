using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;

namespace F1.Abstractions.Network
{
    public interface IMessageCenter
    {
        void RegsiterEvent(Action<IChannel> channelClosedProc,
            Action<OutboundMessage> failMessageProc);
        /// <summary>
        /// 注册消息的回调函数, messageName为""时是默认的回调函数
        /// </summary>
        /// <param name="messageName">消息的名字</param>
        /// <param name="inboundMessage">需要被处理的消息</param>
        void RegisterMessageProc(string messageName, Action<InboundMessage> inboundMessageProc);
        void SendMessage(OutboundMessage message);
        void OnReceiveMessage(InboundMessage message);
        void RegisterUserMessageCallback(Func<string, string, InboundMessage, bool> fn);
        /// <summary>
        /// 收到了一个Actor用户消息, 需要塞到Actor的MailBox里面去顺序处理
        /// </summary>
        /// <param name="type">Actor的类型</param>
        /// <param name="actorID">Actor的ID</param>
        /// <param name="message">收到的消息</param>
        /// <returns>返回这个消息是否被接受了, 不接受的原因可能是内部报错, 或者Actor不在当前的host内</returns>
        bool OnReceiveUserMessage(string type, string actorID, InboundMessage message);
        void OnMessageFail(OutboundMessage message);
        void OnConnectionClosed(IChannel channel);
    }
}
