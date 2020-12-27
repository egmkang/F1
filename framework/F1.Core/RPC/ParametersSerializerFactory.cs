using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using F1.Abstractions.RPC;


namespace F1.Core.RPC
{
    internal class ParametersSerializerFactory : IParametersSerializerFactory
    {
        readonly List<IParametersSerializer> serializers = new List<IParametersSerializer>(128);

        public ParametersSerializerFactory() 
        {
            this.Register((int)Rpc.RpcEncodingType.Ceras, new ParametersSerializerCeras());
            this.Register((int)Rpc.RpcEncodingType.MsgPack, new ParametersSerializerMsgPack());
        }

        public IParametersSerializer GetSerializer(int type)
        {
            if (type < serializers.Count) 
            {
                return serializers[type];
            }
            return null;
        }

        public void Register(int type, IParametersSerializer serializer)
        {
            if (type >= this.serializers.Count) 
            {
                throw new Exception("ParameterSerializer Type Out Of Bound");
            }
            this.serializers[type] = serializer;
        }
    }
}
