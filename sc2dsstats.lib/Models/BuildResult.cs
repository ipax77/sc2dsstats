using System;
using System.Collections.Generic;
using System.Linq;

namespace sc2dsstats.lib.Models
{
    public class BuildResult
    {
        public int TotalGames { get; set; } = 0;
        public int Games { get; set; } = 0;
        public Dictionary<string, float> Units { get; set; } = new Dictionary<string, float>();
        public float Winrate { get; set; } = 0.0f;
        public int Upgradespending { get; set; } = 0;
        public float Gascount { get; set; } = 0;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public Dictionary<int, string> RepIDs { get; set; } = new Dictionary<int, string>();
        public IOrderedEnumerable<KeyValuePair<string, float>> UnitsOrdered;
        public int max1 = 100;
        public int max2 = 100;
        public int max3 = 100;
    }
}
