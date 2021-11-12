using System;
using System.Collections.Generic;

namespace sc2dsstats.lib.Models
{
    public class CmdrInfo
    {
        public double Games { get; set; } = 0;
        public double Wins { get; set; } = 0;
        public double AWinrate { get; set; } = 0;
        public TimeSpan ADuration { get; set; } = TimeSpan.Zero;
        public List<KeyValuePair<string, int>> CmdrCount { get; set; } = new List<KeyValuePair<string, int>>();
        public HashSet<int> GameIDs { get; set; } = new HashSet<int>();

        public CmdrInfo()
        {

        }

        public CmdrInfo(CmdrInfo cp) : this()
        {
            this.Games = cp.Games;
            this.Wins = cp.Wins;
            this.AWinrate = cp.AWinrate;
            this.ADuration = cp.ADuration;
            this.CmdrCount = new List<KeyValuePair<string, int>>(cp.CmdrCount);
        }
    }
}
