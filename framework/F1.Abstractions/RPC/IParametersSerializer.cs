using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions.RPC
{
    public interface IParametersSerializer
    {
        int SerializerType { get; }
        byte[] Serialize(object[] p, Type[] types);
        object[] Deserialize(byte[] bytes, Type[] types);
        byte[] Serialize(object p, Type type);
        object Deserialize(byte[] bytes, Type type);
    }
}
