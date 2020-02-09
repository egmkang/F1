using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using F1.Abstractions.Network;
using F1.Core.Utils;
using Microsoft.Extensions.Logging;

namespace F1.Core.Network
{
    public class DefaultChannelSessionInfoFactory : IConnectionSessionInfoFactory
    {
        private static readonly DateTime RelativeTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private long SessionIDSeed = 0;
        private ILogger logger;

        public DefaultChannelSessionInfoFactory(ILoggerFactory loggerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("F1");
            var relativeSeconds = Platform.GetRelativeSeconds(RelativeTime);
            SessionIDSeed = relativeSeconds  * 10000000000;

            logger.LogInformation("DefaultChannelSessionInfo, SessionIDSeed:{0}, RelativeSeconds:{1}", SessionIDSeed, relativeSeconds);
        }

        public IConnectionSessionInfo NewSessionInfo()
        {
            return new DefaultChannelSessionInfo(Interlocked.Increment(ref this.SessionIDSeed));
        }
    }
}
