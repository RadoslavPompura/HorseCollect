using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorseScraper.Json
{
    [Serializable]
    public class CategoryModel
    {
        public string RaceName { get; set; }
        public List<RaceModel> RaceList { get; set; }
        public string Date { get; set; }
        public string EventId { get; set; }
        public string Country { get; set; }
        public CategoryModel()
        {
            RaceName = "";
            RaceList = new List<RaceModel>();
            Date = "";
            EventId = "";
            Country = "";
        }
    }
}
