using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HorseCollect.Json
{
    public class BetfairMarketItem
    {
        public string eventId { get; set; }
        public string marketId_Win { get; set; }
        public string marketId_Place { get; set; }
        public string marketId_EachWay { get; set; }
        public string marketName { get; set; }
        public string marketType { get; set; }
        public string raceTime { get; set; }
        public string eachwayStr { get; set; }
        public int NumberOfRunners { get; set; }
        public int NumberOfWinners { get; set; }
        public List<BetfairSelectionItem> runnerList { get; set; }
        //public string selectionId { get; set; }
        //public string runnerName { get; set; }
        //public string odds { get; set; }
    }
}