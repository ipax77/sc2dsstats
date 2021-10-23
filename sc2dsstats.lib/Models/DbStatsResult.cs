using System;

namespace sc2dsstats.lib.Models
{
    public class DbStatsResult
    {
        public int DbStatsResultId { get; set; }
        public int Id { get; set; }
        public string Race { get; set; }
        public string OppRace { get; set; }
        public int Duration { get; set; }
        public bool Win { get; set; }
        public bool Player { get; set; }
        public DateTime GameTime { get; set; }
    }
}
