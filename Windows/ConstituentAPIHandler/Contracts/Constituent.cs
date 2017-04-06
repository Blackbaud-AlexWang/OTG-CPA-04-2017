using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConstituentAPIHandler.Contracts
{
    public class Constituent
    {
        public int ConstituentId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string EmailAddress { get; set; }
        public string WebSite { get; set; }
        public DateTime? DateAdded { get; set; }
    }
}