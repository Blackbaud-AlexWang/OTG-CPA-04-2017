using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace ConstituentAPIHandler.Contracts
{
    public class ContractMappers
    {
        public ContractMappers()
        {
            
        }
        public Constituent MapToConstituent(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return new Constituent
            {
                ConstituentId = Convert.ToInt32(serializedResponse["content"]["id"].ToString()),
                FirstName = serializedResponse["content"]["first"].ToString(),
                LastName = serializedResponse["content"]["last"].ToString(),
                Name = SelectName(serializedResponse),
                Age = Convert.ToInt32(serializedResponse["content"]["age"].ToString()),
                EmailAddress = serializedResponse["content"]["email"]["address"].ToString(),
                WebSite = serializedResponse["content"]["online_presence"]["address"].ToString(),
                DateAdded = Convert.ToDateTime(serializedResponse["content"]["date_added"].ToString())
            };
        }
        public int GetConstitID(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return Convert.ToInt32(serializedResponse["content"]["value"].First["id"].ToString());
        }

        private static string SelectName(JObject content)
        {
            return content["content"]["first"] + " " + content["content"]["last"];
        }

        public GivingHistory MapToGivingHistory(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return new GivingHistory
            {
                TotalGiving = Convert.ToDecimal(serializedResponse["content"]["total_giving"]["value"]),
                TotalReceivedGiving = Convert.ToDecimal(serializedResponse["content"]["total_received_giving"]["value"]),
                TotalPledgeBalance = Convert.ToDecimal(serializedResponse["content"]["total_pledge_balance"]["value"]),
                TotalSoftCredits = Convert.ToDecimal(serializedResponse["content"]["total_soft_credits"]["value"]),
                TotalYearsGiven = Convert.ToInt32(serializedResponse["content"]["total_years_given"]),
                ConsecutiveYearsGiven = Convert.ToInt32(serializedResponse["content"]["consecutive_years_given"]),
                TotalCommittedMatchinGifts = Convert.ToInt32(serializedResponse["content"]["total_committed_matching_gifts"]["value"]),
                TotalReceivedMatchingGifts = Convert.ToInt32(serializedResponse["content"]["total_received_matching_gifts"]["value"]),
            };
        }
    }
}