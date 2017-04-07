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
                ConstituentId = Convert.ToInt32(serializedResponse["id"].ToString()),
                FirstName = serializedResponse["first"].ToString(),
                LastName = serializedResponse["last"].ToString(),
                Name = SelectName(serializedResponse),
                Age = Convert.ToInt32(serializedResponse["age"].ToString()),
                EmailAddress = serializedResponse["email"]["address"].ToString(),
                WebSite = serializedResponse["phone"]["number"].ToString(),
                DateAdded = Convert.ToDateTime(serializedResponse["date_added"].ToString())
            };
        }
        public int GetConstitID(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return Convert.ToInt32(serializedResponse["value"].First["id"].ToString());
        }

        private static string SelectName(JObject content)
        {
            return content["first"] + " " + content["last"];
        }

        public GivingHistory MapToGivingHistory(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return new GivingHistory
            {
                TotalGiving = Convert.ToDecimal(serializedResponse["total_giving"]["value"]),
                TotalReceivedGiving = Convert.ToDecimal(serializedResponse["total_received_giving"]["value"]),
                TotalPledgeBalance = Convert.ToDecimal(serializedResponse["total_pledge_balance"]["value"]),
                TotalSoftCredits = Convert.ToDecimal(serializedResponse["total_soft_credits"]["value"]),
                TotalYearsGiven = Convert.ToInt32(serializedResponse["total_years_given"]),
                ConsecutiveYearsGiven = Convert.ToInt32(serializedResponse["consecutive_years_given"]),
                TotalCommittedMatchinGifts = Convert.ToInt32(serializedResponse["total_committed_matching_gifts"]["value"]),
                TotalReceivedMatchingGifts = Convert.ToInt32(serializedResponse["total_received_matching_gifts"]["value"]),
            };
        }
    }
}