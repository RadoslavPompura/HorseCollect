using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorseCollect.Json
{
    [Serializable]
    public class HorseItem_bf
    {
        public string name { get; set; }
        public string odds { get; set; }
        public string layodds { get; set; }
        public string place_odds { get; set; }
        public string place_layodds { get; set; }
    }
}
