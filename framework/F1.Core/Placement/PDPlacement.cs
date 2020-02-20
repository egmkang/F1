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
    public static partial class Extensions
    {
        public static StringContent AsJson(this object o)
            => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }

    public class PDPlacement : IPlacement
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

        public PDPlacement(ILoggerFactory loggerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("F1.Placement");
        }

        public void SetPlacementServerInfo(string address, string domain, List<string> actorType)
        {
            if (address.EndsWith("/")) 
            {
                address = address.Substring(0, address.Length - 1);
            }
            if (!address.StartsWith("http")) 
            {
                address = "http://" + address;
            }
            this.PlacementServerAddress = address;

            this.self.Domain = domain;
            this.self.ActorType = actorType;
            this.logger.LogInformation("SetPlacementServerInfo, Address:{0}, Domain:{1}", address, domain);
        }

        private async Task<ValueTuple<int, string>> GetAsync(string path) 
        {
            var url = $"{this.PlacementServerAddress}{path}";
            var response = await this.httpClient.GetAsync(url);
            var str = await response.Content.ReadAsStringAsync();
            return new ValueTuple<int, string>((int)response.StatusCode, str);
        }

        private async Task<ValueTuple<int, string>> PostAsync(string path, object o) 
        {
            var url = $"{this.PlacementServerAddress}{path}";
            var response = await this.httpClient.PostAsync(url, o.AsJson());
            var str = await response.Content.ReadAsStringAsync();
            return new ValueTuple<int, string>((int)response.StatusCode, str);
        }

        public async Task<PlacementFindActorPositionResponse> FindActorPositonAsync(PlacementFindActorPositionRequest request)
        {
            //TODO
            //check actor position cache

            var (code, str) = await this.PostAsync("/pd/api/v1/actor/find_position", request);
            if (code != (int)HttpStatusCode.OK) 
            {
                this.logger.LogError("FindActorPositionAsync fail, ActorType:{0}, ActorID:{1}, TTL:{2}, ErrorMessage:{3}",
                    request.ActorType, request.ActorID, request.TTL, str);
                throw new PlacementException(code, str); 
            }

            var position = JsonConvert.DeserializeObject<PlacementFindActorPositionResponse>(str);

            //TODO
            //update cache
            return position;
        }

        internal class SequenceResponse 
        {
            public long id;
        }

        private static readonly object EmptyObject = new Dictionary<string, string>();
        public async Task<long> GenerateNewSequenceAsync(string sequenceType, int step)
        {
            var path =$"/pd/api/v1/id/new_sequence/{sequenceType}/{step}";
            var (code, str) = await this.PostAsync(path, EmptyObject.AsJson());

            if (code != (int)HttpStatusCode.OK) 
            {
                this.logger.LogError("GenerateNewSequenceAsync fail, SequenceType:{0}, Step:{1}, ErrorMessage:{3}",
                    sequenceType, step, str);
                throw new PlacementException(code, str);
            }

            var response = JsonConvert.DeserializeObject<SequenceResponse>(str);
            return response.id;
        }

        public async Task<long> GenerateNewTokenAsync()
        {
            var path  = "/pd/api/v1/actor/new_token";
            var (code, str) = await this.PostAsync(path, EmptyObject.AsJson());

            if (code != (int)HttpStatusCode.OK) 
            {
                this.logger.LogError("GenerateNewTokenAsync fail, ErrorMessage:{0}", str);
                throw new PlacementException(code, str);
            }

            var response = JsonConvert.DeserializeObject<SequenceResponse>(str);
            return response.id;
        }

        public async Task<long> GenerateServerIDAsync()
        {
            var path  = "/pd/api/v1/id/new_server_id";
            var (code, str) = await this.PostAsync(path, EmptyObject.AsJson());

            if (code != (int)HttpStatusCode.OK) 
            {
                this.logger.LogError("GenerateServerIDAsync fail, ErrorMessage:{0}", str);
                throw new PlacementException(code, str);
            }

            var response = JsonConvert.DeserializeObject<SequenceResponse>(str);
            return response.id;
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

        public async Task<PlacementVersionInfo> GetVersionAsync()
        {
            var (code, str) = await this.GetAsync("/pd/api/v1/version");

            if (code != (int)HttpStatusCode.OK) 
            {
                this.logger.LogError("GetVersion fail, ErrorMessage:{0}", str);
                throw new PlacementException(code, str);
            }

            var position = JsonConvert.DeserializeObject<PlacementVersionInfo>(str);
            return position;
        }
    }
}
