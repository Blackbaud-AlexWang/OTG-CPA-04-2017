using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ConstituentAPIHandler.API;
using ConstituentAPIHandler.Contracts;
using Newtonsoft.Json.Linq;

namespace CSHttpClientSample
{
    public class ConstituentHandler
    {
        private static HttpClient _client;
        private static Headers headers;

        public ConstituentHandler(HttpClient client)
        {
            _client = client;
            _client.DefaultRequestHeaders.Add("bb-api-subscription-key", headers.SubscriptionKey);
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {headers.AccessKey}");
            headers = new Headers();
        }

        public async Task<Constituent> GetConstituent(int constituentId)
        {
            var uri = $"https://api.sky.blackbaud.com/constituent/v1/constituents/{constituentId}";
            var response = await _client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.Unauthorized) { throw new Exception("Unauthorized, submit new access key"); }
            var result = response.Content;
            return MapToConstituent(result);
        }

        public async Task<int> SearchConstituent(string name)
        {
            var uri = $"https://api.sky.blackbaud.com/constituent/v1/constituents/search?search_text={name}";

            var response = await _client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.Unauthorized) {  throw new Exception("Unauthorized, submit new access key");}
            var result = response.Content;
            return GetConstitID(result);
        }

        private static Constituent MapToConstituent(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return new Constituent
            {
                ConstituentId = Convert.ToInt32(serializedResponse["content"]["id"].ToString()),
                PreferredName = serializedResponse["content"]["preferred_name"].ToString(),
                LastName = serializedResponse["content"]["last_name"].ToString(),
                Name = SelectName(serializedResponse),
                Age = Convert.ToInt32(serializedResponse["content"]["age"].ToString()),
                EmailAddress = serializedResponse["content"]["email"]["address"].ToString(),
                WebSite = serializedResponse["content"]["online_presence"]["address"].ToString(),
                DateAdded = Convert.ToDateTime(serializedResponse["content"]["date_added"].ToString())
            };
        }
        private static int GetConstitID(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return Convert.ToInt32(serializedResponse["content"]["value"].First["id"].ToString());
        }

        private static string SelectName(JObject content)
        {
            return content["content"]["preferred_name"] + " " + content["content"]["last_name"];
        }
    }
}