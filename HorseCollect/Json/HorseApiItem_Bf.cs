using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Web;

namespace HorseCollect.Json
{
    public class HorseApiItem_Bf
    {
        public string raceName { get; set; }
        public string raceTime { get; set; }
        public string runnerName { get; set; }
        public string odds { get; set; }
        public string odds_fraction { get; set; }
        public double Rating { get; set; }
        public string bfWinLay { get; set; }
        public string bfWinBack { get; set; }
        public string bfPlaceLay { get; set; }
        public string bfPlaceBack { get; set; }
        public string bfEachWayLay { get; set; }
        public string bfEachWayBack { get; set; }
        public string bfFavPrice { get; set; }
        public string bfMarketId_Win { get; set; }
        public string bfMarketId_Place { get; set; }
        public string bfSelectionId { get; set; }
        public string winAvailableMoney { get; set; }
        public string placeAvailableMoney { get; set; }
        public int numberOfRunners { get; set; }
        public int numberOfWinners { get; set; }

        public string ew_divisor { get; set; }
    }
}