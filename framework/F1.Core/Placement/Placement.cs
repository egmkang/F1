using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using F1.Abstractions.Placement;
using F1.Core.Utils;

namespace F1.Core.Placement
{
    public static class Extensions
    {
        public static StringContent AsJson(this object o)
            => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }

    struct ResponseStatus 
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class Placement : IPlacement
    {
        private readonly HttpClient httpClient = new HttpClient(new HttpClientHandler()
        {
            Proxy = null,
            UseProxy = false,
        });
        private readonly ILogger logger;
        private readonly LRU<string, PlacementFindActorPositionResponse> lru = new LRU<string, PlacementFindActorPositionResponse>(10 * 10000);
        private readonly PlacementActorHostInfo self = new PlacementActorHostInfo();
        public string PlacementServerAddress { get; private set; }

        public string Domain => this.self.Domain;

        public Placement(ILoggerFactory loggerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Placement");
        }

        public void SetPlacementServerInfo(string address, string domain, List<string> actorType)
        {
            if (address.EndsWith("/")) 
            {
                address = address.Substring(0, address.Length - 1);
            }
            this.PlacementServerAddress = address;

            this.self.Domain = domain;
            this.self.ActorType = actorType;
            this.logger.LogInformation("SetPlacementServerInfo, Address:{0}, Domain:{1}", address, domain);
        }

        private async Task<ValueTuple<int, string>> PostAsync(string path, object o) 
        {
            var json = JsonConvert.SerializeObject(o);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{this.self.Address}/{path}";
            var response = await this.httpClient.PostAsync(url, content);
            var str = await response.Content.ReadAsStringAsync();
            return new ValueTuple<int, string>((int)response.StatusCode, str);
        }

        public async Task<PlacementFindActorPositionResponse> FindActorPositonAsync(PlacementFindActorPositionRequest request)
        {
            var (code, str) = await this.PostAsync("/pd/api/v1/actor/find_position", request);
            if (code != 0) 
            {
                throw new PlacementException(code, str);
            }

            var position = JsonConvert.DeserializeObject<PlacementFindActorPositionResponse>(str);

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

        public Task<long> RegisterServerAsync(PlacementActorHostInfo info)
        {
            throw new NotImplementedException();
        }

        private bool IsActorPositionValid(PlacementFindActorPositionResponse info) 
        {
            return false;
        }
    }
}
