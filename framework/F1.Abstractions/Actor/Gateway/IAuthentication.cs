using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions.Actor.Gateway
{
    public interface IAuthentication
    {
        object DecodeToken(byte[] token);
    }
}
