using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using F1.Abstractions.Actor;
using F1.Abstractions.Network;
using F1.Core.RPC;

namespace F1.Core.Actor
{
    public abstract class Actor : IActor
    {
        public Type ActorType { get; private set; }
        public string ID { get; private set; }
        public string UniqueID { get; private set; }
        internal IActorContext Context { get; set; }
        internal RpcDispatchProxyFactory ProxyFactory { get; set; }
        public ILogger Logger { get; internal set; }
        private ActorTimerManager TimerManager { get; set; }

        internal void InitActor(Type type, string id, IActorContext context)
        {
            this.ActorType = type;
            this.ID = id;
            this.Context = context;
            this.UniqueID = $"{this.ActorType.Name}@{this.ID}";
            this.TimerManager = new ActorTimerManager(this);
        }

        internal async Task ActivateAsync() 
        {
            try 
            {
                await this.OnActivateAsync();
                this.Logger.LogInformation("ActorActiveAsync success, ID:{0}", this.UniqueID);
            }
            catch (Exception e) 
            {
                this.Logger.LogError("ActorActivateAsync fail, ID:{0}, Exception:{1}", this.UniqueID, e.ToString());
            }
        }
        protected virtual Task OnActivateAsync() 
        {
            return Task.CompletedTask;
        }

        internal async Task DeactivateAsync() 
        {
            try 
            {
                await this.OnDeactivateASync();
                this.Logger.LogInformation("ActorDeactivateAsync success, ID:{0}", this.UniqueID);
            }
            catch (Exception e)
            {
                this.Logger.LogError("ActorDeactivateAsync fail, ID:{0}, Exception:{1}", this.UniqueID, e.ToString());
            }
        }
        protected virtual Task OnDeactivateASync() 
        {
            return Task.CompletedTask;
        }

        protected virtual Task ProcessInputMessage(InboundMessage msg) 
        {
            return Task.CompletedTask;
        }

        public T GetActorProxy<T>(string name) 
        {
            return this.ProxyFactory.CreateProxy<T>(name, this.Context);
        }

        /// <summary>
        /// 用户自己派发消息
        /// </summary>
        /// <param name="inboundMessage">进来的消息</param>
        /// <returns>返回值, 只能是Task类型</returns>
        internal virtual Task DispatchUserMessage(InboundMessage inboundMessage)
        {
            return Task.CompletedTask;
        }

        public ActorTimer RegisterTimer(Func<ActorTimer, Task> fn, int interval) 
        {
            return this.TimerManager.RegisterTimer(interval, fn);
        }

        public void UnRegisterTimer(ActorTimer timer)
        {
            this.UnRegisterTimer(timer.ID);
        }

        public void UnRegisterTimer(long id) 
        {
            this.TimerManager.UnRegisterTimer(id);
        }
    }
}
