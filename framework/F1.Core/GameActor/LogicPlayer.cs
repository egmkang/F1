using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using F1.Core.RPC;

namespace F1.Core.GameActor
{
    /// <summary>
    /// 处理玩家消息的主体对象, Player和Gateway上面的Session绑定, Gateway通知最新的消息到Player对象上
    /// 处理玩家的消息(主要逻辑消息), 需要实现该接口
    /// </summary>
    [Rpc]
    public interface ILogicPlayer
    {
        Task OnNewConnection(long newSessionID, string token);
        Task OnConnectionAborted(long sessionID);
        Task CloseConnection();

        Task OnNewMessage(byte[] rawMessage);
        Task SendMessageToPlayer(object message);
    }
}
