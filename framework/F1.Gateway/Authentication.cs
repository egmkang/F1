using System;
using System.Collections.Generic;
using System.Text;
using F1.Abstractions.Actor.Gateway;

namespace F1.Gateway
{
    internal class Authentication : IAuthentication
    {
        public object DecodeToken(byte[] token)
        {
            return Encoding.UTF8.GetString(token);
        }
    }
}
