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
            ConfigureClient(_client);
            _mapper = new ContractMappers();
        }

        private static void ConfigureClient(HttpClient client)
        {
            
            client.DefaultRequestHeaders.Add("bb-api-subscription-key", Headers.SubscriptionKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Headers.AccessKey}");
            client.BaseAddress = new Uri("https://api.sky.blackbaud.com/constituent/v1/constituents/");
        }

        public Constituent GetConstituent(int constituentId)
        {
            var thing = Headers.AccessKey;
            var uri = _client.BaseAddress + constituentId.ToString();
            var response =  _client.GetAsync(uri).Result;
            var stringData = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode == HttpStatusCode.Unauthorized) { throw new Exception("Unauthorized, submit new access key"); }
            var result = response.Content;
            return _mapper.MapToConstituent(result);
        }
        public GivingHistory GetGivingHistory(int constituentId)
        {
            var uri = _client.BaseAddress +  $"{constituentId}/givingsummary/lifetimegiving";
            var response = _client.GetAsync(uri).Result;
            if (response.StatusCode == HttpStatusCode.Unauthorized) { throw new Exception("Unauthorized, submit new access key"); }
            var result = response.Content;
            return _mapper.MapToGivingHistory(result);
        }

        public int SearchConstituent(string name)
        {
            var uri = _client.BaseAddress + $"search?search_text={name}";
            var response = _client.GetAsync(uri).Result;
            if (response.StatusCode == HttpStatusCode.Unauthorized) {  throw new Exception("Unauthorized, submit new access key");}
            var result = response.Content;
            return _mapper.GetConstitID(result);
        }

    }
}