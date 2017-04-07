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
                Name = SelectName(serializedResponse),
                Phone = serializedResponse["phone"]["number"].ToString()
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

        public LastGift MapToLastGift(HttpContent content)
        {
            var responseDate = content.ReadAsStringAsync().Result;
            var serializedResponse = JObject.Parse(responseDate);
            return new LastGift
            {
                DateGiven = Convert.ToDateTime(serializedResponse["date"]),
                Amount = Convert.ToDecimal(serializedResponse["amount"]["value"]),
                Type = serializedResponse["type"].ToString(),
            };
        }

    }
}