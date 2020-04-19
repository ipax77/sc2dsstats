using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace paxgamelib.Models
{
    [Serializable]
    public class GameMode
    {
        public string Vs { get; set; }
        public string Mode { get; set; } = "Bot#1";
        public string Difficulty { get; set; } = "Expert";
    }
}
