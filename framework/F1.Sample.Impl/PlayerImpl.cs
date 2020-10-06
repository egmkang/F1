using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using F1.Core.Actor;
using F1.Sample.Interface;
using F1.Abstractions.Network;

namespace F1.Sample.Impl
{
    class PlayerImpl : Actor, IPlayer
    {
        public Task AuthTokenAsync(byte[] token)
        {
            try 
            {
                var playerId = Encoding.UTF8.GetString(token);
                this.Logger.LogInformation("PlayerID:{0}, Token:{1}", this.ID, playerId);
            }
            catch (Exception e) 
            {
                this.Logger.LogError("PlayerID:{0}, AuthTokenAsync, Exception:{1}", this.ID, e);
            }
            return Task.CompletedTask;
        }

        public Task<string> SayHelloAsync(string name)
        {
            this.Logger.LogInformation("PlayerID:{0} SayHelloAsync, Name:{1}", this.ID, name);
            return Task.FromResult(name);
        }

        protected override async Task ProcessUserInputMessage(InboundMessage msg)
        {
            //TODO
            await Task.Yield();
        }
    }
}
