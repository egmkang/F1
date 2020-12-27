using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F1.Abstractions.RPC
{
    public interface IParametersSerializerFactory
    {
        void Register(int type, IParametersSerializer serializer);
        IParametersSerializer GetSerializer(int type);
    }
}
