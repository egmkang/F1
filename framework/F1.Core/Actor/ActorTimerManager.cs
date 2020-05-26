﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;


namespace F1.Core.Actor
{
    internal class ActorTimerManager
    {
        private readonly Actor actor;
        private readonly Dictionary<long, ActorTimer> timers = new Dictionary<long, ActorTimer>();
        private static readonly object Mutex = new object();
        private static readonly HashedWheelTimer HashedWheelTimer = new HashedWheelTimer(TimeSpan.FromMilliseconds(50), 1024, -1);

        public ActorTimerManager(Actor actor) 
        {
            this.actor = actor;
        }

        internal ActorTimer RegisterTimer(int interval, Func<ActorTimer, Task> fn)
        {
            var timer = new ActorTimer(this.actor, fn, this, interval);
            this.timers.Add(timer.ID, timer);

            var nextWait = timer.GetNextWaitTime();
            this.RegisterTimer(timer, nextWait);
            return timer;
        }

        internal void RegisterTimer(ActorTimer timer, long nextWait)
        {
            lock (Mutex) HashedWheelTimer.NewTimeout(timer, TimeSpan.FromMilliseconds(nextWait));
        }

        internal void UnRegisterTimer(long id) 
        {
            this.timers.Remove(id, out var timer);
            if (timer != null) 
            {
                timer.MarkCancel();
            }
        }
    }
}