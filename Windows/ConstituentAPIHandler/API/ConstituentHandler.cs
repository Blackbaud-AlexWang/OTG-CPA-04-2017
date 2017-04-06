using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ConstituentAPIHandler.API;
using ConstituentAPIHandler.Contracts;

namespace CSHttpClientSample
{
    public class ConstituentHandler
    {
        private static HttpClient _client;
        private static ContractMappers _mapper;

        public ConstituentHandler(HttpClient client)
        {
            _client = client;
            _client.DefaultRequestHeaders.Add("bb-api-subscription-key", Headers.SubscriptionKey);
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Headers.AccessKey}");
            _mapper = new ContractMappers();
        }

        public async Task<Constituent> GetConstituent(int constituentId)
        {
            var uri = $"https://api.sky.blackbaud.com/constituent/v1/constituents/{constituentId}";
            var response = await _client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.Unauthorized) { throw new Exception("Unauthorized, submit new access key"); }
            var result = response.Content;
            return _mapper.MapToConstituent(result);
        }
        public async Task<GivingHistory> GetGivingHistory(int constituentId)
        {
            var uri = $"https://api.sky.blackbaud.com/constituent/v1/constituents/{constituentId}/givingsummary/lifetimegiving";
            var response = await _client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.Unauthorized) { throw new Exception("Unauthorized, submit new access key"); }
            var result = response.Content;
            return _mapper.MapToGivingHistory(result);
        }

        public async Task<int> SearchConstituent(string name)
        {
            var uri = $"https://api.sky.blackbaud.com/constituent/v1/constituents/search?search_text={name}";

            var response = await _client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.Unauthorized) {  throw new Exception("Unauthorized, submit new access key");}
            var result = response.Content;
            return _mapper.GetConstitID(result);
        }

    }
}