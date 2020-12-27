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
        public long SessionID { get; private set; }
        internal IActorContext Context { get; set; }
        internal RpcDispatchProxyFactory ProxyFactory { get; set; }
        public ILogger Logger { get; internal set; }
        private ActorTimerManager TimerManager { get; set; }
        public IMessageCenter MessageCenter { get; private set; }

        internal Func<string> NewSourceReentrantId { get; set; } 

        internal void InitActor(Type type, string id, IActorContext context, IMessageCenter messageCenter)
        {
            this.ActorType = type;
            this.ID = id;
            this.Context = context;
            this.UniqueID = $"{this.ActorType.Name}@{this.ID}";
            this.TimerManager = new ActorTimerManager(this);
            this.MessageCenter = messageCenter;
        }

        internal async Task ActivateAsync() 
        {
            try 
            {
                await this.OnActivateAsync().ConfigureAwait(false);
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
                await this.OnDeactivateAsync().ConfigureAwait(false);
                this.Logger.LogInformation("ActorDeactivateAsync success, ID:{0}", this.UniqueID);
            }
            catch (Exception e)
            {
                this.Logger.LogError("ActorDeactivateAsync fail, ID:{0}, Exception:{1}", this.UniqueID, e.ToString());
            }
        }
        protected virtual Task OnDeactivateAsync() 
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 用户自己派发消息
        /// </summary>
        /// <param name="inboundMessage">进来的消息</param>
        /// <returns>返回值, 只能是Task类型</returns>
        protected virtual Task ProcessUserInputMessage(InboundMessage msg) 
        {
            return Task.CompletedTask;
        }

        public T GetActorProxy<T>(string name) 
        {
            return this.ProxyFactory.CreateProxy<T>(name, this.Context);
        }

        internal Task DispatchUserMessage(InboundMessage inboundMessage)
        {
            return this.ProcessUserInputMessage(inboundMessage);
        }

        public ActorTimer RegisterTimer(Action<ActorTimer> fn, int interval) 
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

        public void SetSessionID(long newSessionID) 
        {
            if (this.SessionID != newSessionID) 
            {
                this.Logger.LogInformation("Actor:{0}/{1}, SessionID:{2} => NewSessionID:{3}",
                    this.ActorType, this.ID, this.SessionID, newSessionID);
                var oldId = this.SessionID;
                this.SessionID = newSessionID;
                try 
                {
                    this.OnSessionIDChanged(oldId, newSessionID);
                }
                catch { }
            }
        }

            //在这边更新可重入ID
        public void BeforeProcessUserMessage() 
        {
            this.Context.ReentrantId = this.NewSourceReentrantId();
        }
        public void EndProcessUserMessage() 
        {
            this.Context.ReentrantId = "";
        }

        protected virtual void OnSessionIDChanged(long oldId, long newId)
        {
        }
    }
}
