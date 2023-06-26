using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorseScraper.Json
{
    [Serializable]
    public class HorseModel
    {
        public string HorseName { get; set; }
        public string CardNum { get; set; }
        public string SelectionId { get; set; }
        public string Win_value { get; set; }
        public string Place_value { get; set; }
        public string Place4_value { get; set; }
        public string Place2_value { get; set; }
        public string Place3_value { get; set; }
        public string Place5_value { get; set; }
        public string MatchPercent { get; set; }

        public HorseModel()
        {
            HorseName = "";
            CardNum = "";
            SelectionId = "";
            Win_value = "";
            Place_value = "";
            Place4_value = "";
            Place2_value = "";
            Place3_value = "";
            Place5_value = "";
            MatchPercent = "0";
        }
    }
}
