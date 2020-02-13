using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using F1.Abstractions.Network;
using F1.Core.Message;
using F1.Core.Utils;
using Microsoft.Extensions.Logging;

namespace F1.Core.Network
{
    public sealed class DefaultConnectionSessionInfoFactory : IConnectionSessionInfoFactory
    {
        private static readonly DateTime RelativeTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private long SessionIDSeed = 0;
        private readonly ILogger logger;
        private readonly IMessageCenter messageCenter;

        public DefaultConnectionSessionInfoFactory(ILoggerFactory loggerFactory, IMessageCenter messageCenter) 
        {
            this.logger = loggerFactory.CreateLogger("F1.SessionInfo");
            this.messageCenter = messageCenter;
            var relativeSeconds = Platform.GetRelativeSeconds(RelativeTime);
            SessionIDSeed = relativeSeconds  * 10000000000;

            logger.LogInformation("DefaultChannelSessionInfo, SessionIDSeed:{0}, RelativeSeconds:{1}",
                SessionIDSeed, relativeSeconds);
        }

        public IConnectionSessionInfo NewSessionInfo(IMessageHandlerFactory handlerFactory)
        {
            return new DefaultConnectionSessionInfo(Interlocked.Increment(ref this.SessionIDSeed),
                this.logger, this.messageCenter, handlerFactory.Codec);
        }
    }
}
