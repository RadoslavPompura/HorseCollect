using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HorseCollect.Json
{
    public class BetfairResItem
    {
        public string eventName { get; set; }
        public string eventId { get; set; }
        public string countryCode { get; set; }
        public DateTime openTime { get; set; }
        public int marketCounts { get; set; }
        public List<BetfairMarketItem> marketList { get; set; }
    }
}