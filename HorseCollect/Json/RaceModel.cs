using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorseScraper.Json
{
    [Serializable]
    public class RaceModel
    {
        public string RaceTime { get; set; }

        public List<HorseModel> HorseList { get; set; }

        public string Win_MarketId { get; set; }

        public string Place_MarketId { get; set; }

        public string Place4_MarketId { get; set; }

        public string Place2_MarketId { get; set; }

        public string Place3_MarketId { get; set; }

        public string Place5_MarketId { get; set; }

        public string StartTime { get; set; }

        public RaceModel()
        {
            RaceTime = "";
            HorseList = new List<HorseModel>();
            Win_MarketId = "";
            Place_MarketId = "";
            Place4_MarketId = "";
            Place2_MarketId = "";
            Place3_MarketId = "";
            Place5_MarketId = "";
            StartTime = "";
        }
    }
}
