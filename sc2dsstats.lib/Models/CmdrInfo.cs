using System;
using System.Collections.Generic;
using System.Text;

namespace sc2dsstats.lib.Models
{
    public class CmdrInfo
    {
        public string Cmdr { get; set; }
        public int Games { get; set; } = 0;
        public int Matchups { get; set; } = 0;
        public string Winrate { get; set; } = "0";
        public string AverageGameDuration { get; set; } = "0";
        public string FilterInfo { get; set; } = "";
        public Dictionary<string, int> CmdrCount { get; set; } = new Dictionary<string, int>();


    }
}
