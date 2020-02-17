using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using F1.Abstractions.Placement;
using Microsoft.Extensions.Logging;

namespace F1.Core.Placement
{
    public class Placement : IPlacement
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly ILogger logger;
        private string domain;
        private string placementAddress;

        public string PlacementServerAddress => this.placementAddress;

        public string Domain => this.domain;

        public Placement(ILoggerFactory loggerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Placement");
        }

        public void SetPlacementServerInfo(string address, string domain)
        {
            this.domain = domain;
            this.placementAddress = address;
            this.logger.LogInformation("SetPlacementServerInfo, Address:{0}, Domain:{1}", address, domain);
        }

        public Task<PlacementFindActorPositionResponse> FindActorPositonAsync(PlacementFindActorPositionRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<long> GenerateNewSequenceAsync(string sequenceType, int step)
        {
            throw new NotImplementedException();
        }

        public Task<long> GenerateNewTokenAsync()
        {
            throw new NotImplementedException();
        }

        public Task<long> GenerateServerIDAsync()
        {
            throw new NotImplementedException();
        }

        public Task<PlacementKeepAliveResponse> KeepAliveServerAsync(long serverID, long leaseID, int load)
        {
            throw new NotImplementedException();
        }

        public Task<long> RegisterServerAsync(long serverID, string domain)
        {
            throw new NotImplementedException();
        }
    }
}
