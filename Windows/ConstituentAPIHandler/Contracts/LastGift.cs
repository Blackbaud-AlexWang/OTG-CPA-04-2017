using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConstituentAPIHandler.Contracts
{
    public class LastGift
    {
        public DateTime? DateGiven { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
    }
}