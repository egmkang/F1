using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotNetty.Common.Utilities;
using F1.Abstractions.Network;
using F1.Core.Utils;

namespace F1.Core.Actor
{
    public class ActorTimer: ITimerTask
    {
        private static long IdSequence = 1;
        private static long NewID => Interlocked.Increment(ref IdSequence);

        private readonly long id;
        private readonly Func<ActorTimer, Task> fn;
        private readonly Actor actor;
        private readonly ActorTimerManager manager;
        private readonly ILogger logger;
        private long beginMilliSeconds;
        private long tickCount;
        private readonly long interval;
        private bool cancel = false;

        internal ActorTimer(Actor actor, Func<ActorTimer, Task> fn, ActorTimerManager manager, int interval)
        {
            this.id = NewID;
            this.actor = actor;
            this.fn = fn;
            this.manager = manager;
            this.logger = actor.Logger;
            this.beginMilliSeconds = Platform.GetMilliSeconds();
            this.tickCount = 0;
            this.interval = interval;
        }

        public long ID => this.id;
        public long BeginMilliSeconds => this.beginMilliSeconds;
        public long Interval => this.interval;
        public long TickCount => this.tickCount;
        public bool IsCancel => this.cancel;

        internal async Task Tick() 
        {
            if (this.IsCancel) return;

            try
            {
                this.tickCount++;
                await this.fn(this);
            }
            catch (Exception e)
            {
                this.logger.LogError("Actor:{0}, ExecTimerFunc fail, Exception:{1}", actor.UniqueID, e.ToString());
                return;
            }
            if (!this.IsCancel) 
            {
                var nextWait = this.GetNextWaitTime();
                this.manager.RegisterTimer(this, nextWait);
            }
        }

        internal long GetNextWaitTime() 
        {
            var currentTime = Platform.GetMilliSeconds();
            var nextTime = this.beginMilliSeconds + (this.tickCount + 1) * this.interval;
            var waitTime = nextTime - currentTime;
            return waitTime > 0 ? waitTime : 0;
        }

        internal void MarkCancel() 
        {
            this.cancel = true;
        }

        public void Run(ITimeout timeout)
        {
            this.actor.Context.SendMail(new InboundMessage(null, "timer", this, Platform.GetMilliSeconds()));
        }
    }
}
