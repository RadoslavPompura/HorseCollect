using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HorseCollect.Json
{
    public class BetfairSelectionItem
    {
        public string selectionId { get; set; }
        public string runnnerName { get; set; }
        public double winLayOdds { get; set; }
        public double winBackOdds { get; set; }
        public double placeLayOdds { get; set; }
        public double placeBackOdds { get; set; }
        public double eachWayLayOdds { get; set; }
        public double eachWayBackOdds { get; set; }
        public double favPrice { get; set; }
        public double amountWinAvailableToLay { get; set; }
        public double amountPlaceAvailableToLay { get; set; }
    }
}