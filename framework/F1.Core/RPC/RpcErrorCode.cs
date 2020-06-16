using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.RPC
{
    public enum RpcErrorCode 
    {
        MethodNotFound           = 1,   //方法没找到
        ActorHasNewPosition      = 2,   //Actor有新的位置
        ActorPositionNotFound    = 3,   //Actor位置没有找到(极端情况下可能存在)
        Others                   = 100, //其他错误
    }
}
