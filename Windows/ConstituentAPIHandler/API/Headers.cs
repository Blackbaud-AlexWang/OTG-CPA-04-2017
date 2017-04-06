using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConstituentAPIHandler.API
{
    public static class Headers
    {
        public static string SubscriptionKey = "e87327787bbd4c719d6c95ba44a0d0a4";

        //access key comes from sky api developer portal. it lasts one hour. don't know better way to get it
        public static string AccessKey { get; set; }
        
    }
}