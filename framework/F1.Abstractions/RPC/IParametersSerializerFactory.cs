using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F1.Abstractions.RPC
{
    public interface IParametersSerializerFactory
    {
        //可以注册默认参数序列化方式, type填0即可
        void Register(int type, IParametersSerializer serializer);
        // type 0会使用默认参数序列化方式
        IParametersSerializer GetSerializer(int type);
    }
}
