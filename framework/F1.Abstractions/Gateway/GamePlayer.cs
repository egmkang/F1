using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using F1.Abstractions.RPC;

namespace F1.Abstractions.GameActor
{
    /// <summary>
    /// 游戏玩家对象, 比如一场战斗内的玩家对象, 和LogicPlayer可以在物理上分开
    /// </summary>
    [Rpc]
    public interface IGamePlayer
    {
        Task OnNewConnection(long newSessionID, string token);
        Task OnConnectionAborted(long sessionID);
        Task CloseConnection();

        Task OnNewMessage(byte[] rawMessage);
        Task SendMessageToPlayer(object message);
    }
}
