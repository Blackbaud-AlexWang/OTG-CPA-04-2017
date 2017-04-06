using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConstituentAPIHandler.Contracts
{
    public class GivingHistory
    {

        public decimal? TotalGiving { get; set; }
        public decimal? TotalReceivedGiving { get; set; }
        public decimal? TotalPledgeBalance { get; set; }
        public decimal? TotalSoftCredits { get; set; }
        public int TotalYearsGiven { get; set; }
        public int ConsecutiveYearsGiven { get; set; }
        public int TotalCommittedMatchinGifts { get; set; }
        public int TotalReceivedMatchingGifts { get; set; }
    }
}