using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace F1.Abstractions.Placement
{
    /// <summary>
    /// PD服务器上, Actor宿主服务器的信息
    /// </summary>
    public class PlacementActorHostInfo 
    {
        /// <summary>
        /// 服务器唯一ID
        /// </summary>
        public long ServerID = 0;
        /// <summary>
        /// 租约
        /// </summary>
        public long LeaseID = 0;
        /// <summary>
        /// 服务器的负载(运行时也就只有负载可变, 其他信息都不允许发生变化)
        /// </summary>
        public long Load = 0;
        /// <summary>
        /// 服务器启动时间, 相对于UTC的毫秒数
        /// </summary>
        public long StartTime = 0;
        /// <summary>
        /// 服务器的生存期
        /// </summary>
        public long TTL = 0;
        /// <summary>
        /// 服务器的地址
        /// </summary>
        public string Address = "";
        /// <summary>
        /// 服务器的名字空间, 用来做多组服务器隔离的, PD上面有名字空间, framework里面没有
        /// framework不允许多个名字空间的服务器进行交互
        /// </summary>
        public string Domain = "";
        /// <summary>
        /// 服务器能提供的Actor对象类型, 即服务能力
        /// </summary>
        public List<string> ActorType = new List<string>();
    }

    /// <summary>
    /// PD上最近发生的事件
    /// </summary>
    public class PlacementEvents
    {
        /// <summary>
        /// 事件的事件
        /// </summary>
        public long Time = 0;
        /// <summary>
        /// 增加的服务器ID
        /// </summary>
        public List<long> Add = new List<long>();
        /// <summary>
        /// 删除的服务器ID
        /// </summary>
        public List<long> Remove = new List<long>();
    }

    /// <summary>
    /// 服务器续约返回
    /// </summary>
    public class PlacementKeepAliveResponse 
    {
        /// <summary>
        /// 每次续约PD会将所有的服务器信息下发, 该framework所能处理的集群规模, 也就是百十来台, 所以将所有服务器下发没有问题
        /// </summary>
        public Dictionary<long, PlacementActorHostInfo> Hosts = new Dictionary<long, PlacementActorHostInfo>();
        /// <summary>
        /// 服务器最近的事件(增减和删除)
        /// </summary>
        public List<PlacementEvents> Events = new List<PlacementEvents>();
    }

    /// <summary>
    /// PD上对于Actor定位的请求
    /// </summary>
    public class PlacementFindActorPositionRequest 
    {
        /// <summary>
        /// 名字空间
        /// </summary>
        public string Domain = "";
        /// <summary>
        /// Actor的类型, 参见ActorType
        /// </summary>
        public string ActorType = "";
        /// <summary>
        /// Actor的ID, 在该ActorType下必须唯一
        /// </summary>
        public string ActorID = "";
        /// <summary>
        /// Actor的生命周期, 通常为0, 那么Actor的宿主挂掉之后, PD会寻求再次分配新的位置
        /// 不为0时, 那么Actor的宿主挂掉之后, PD不会再次分配新的位置. 通常用来做一次性定位, 比如一场战斗, 战斗服务器挂掉后很难恢复.
        /// </summary>
        public long TTL = 0;
    }

    /// <summary>
    /// PD对Actor定位的返回
    /// </summary>
    public class PlacementFindActorPositionResponse 
    {
        /// <summary>
        /// 名字空间
        /// </summary>
        public string Domain = "";
        /// <summary>
        /// Actor的类型, 参见ActorType
        /// </summary>
        public string ActorType = "";
        /// <summary>
        /// ActorType类型下唯一的ID
        /// </summary>
        public string ActorID = "";
        /// <summary>
        /// 生存期
        /// </summary>
        public long TTL = 0;
        /// <summary>
        /// Actor分配的时间
        /// </summary>
        public long CreateTime = 0;
        /// <summary>
        /// 宿主的唯一ID
        /// </summary>
        public long ServerID = 0;
        /// <summary>
        /// 宿主的地址
        /// </summary>
        public string ServerAddress = "";
    }

    public interface IPlacement
    {
        string PlacementServerAddress { get; }
        string Domain { get; }

        /// <summary>
        /// 设置PlacementDriver服务器的信息
        /// </summary>
        /// <param name="address">PD服务器的地址</param>
        /// <param name="domain">该framework的名字空间, 调试的时候用来做隔离</param>
        /// <param name="actorType">该服务器所能提供的Actor服务类型</param>
        void SetPlacementServerInfo(string address, string domain, List<string> actorType);
        
        /// <summary>
        /// 生成一个新的服务器ID, 服务器每次启动的时候都需要向PD去申请新的ID
        /// </summary>
        /// <returns>返回新的唯一ID</returns>
        Task<long> GenerateServerIDAsync();
        /// <summary>
        /// 获取一个新的ID, 可以提供比较频繁的调用
        /// </summary>
        /// <param name="sequenceType">类型</param>
        /// <param name="step">步长, 内部一次申请步长个ID, 缓冲起来, 提高性能用的参数</param>
        /// <returns>返回一个SequenceID</returns>
        Task<long> GenerateNewSequenceAsync(string sequenceType, int step);
        /// <summary>
        /// 生成一个新的写入Token, 用来做Actor写入权限的判断
        /// </summary>
        /// <returns>生成新的Token</returns>
        Task<long> GenerateNewTokenAsync();
        /// <summary>
        /// 注册当前服务器到PD里面去
        /// </summary>
        /// <returns>返回租约</returns>
        Task<long> RegisterServerAsync(PlacementActorHostInfo info);
        /// <summary>
        /// 给当前服务器续约, 维持其生命
        /// </summary>
        /// <param name="serverID">服务器ID</param>
        /// <param name="leaseID">租约ID</param>
        /// <param name="load">服务器当前的负载</param>
        /// <returns>返回PD上面最新的事件</returns>
        Task<PlacementKeepAliveResponse> KeepAliveServerAsync(long serverID, long leaseID, int load);
        /// <summary>
        /// 找到Actor所在的服务器信息
        /// </summary>
        /// <param name="request">actor定位所需要的信息</param>
        /// <returns>Actor所在的目标服务器信息</returns>
        Task<PlacementFindActorPositionResponse> FindActorPositonAsync(PlacementFindActorPositionRequest request);
    }
}
