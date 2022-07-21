using System.Collections.Generic;

namespace sc2dsstats._2022.Shared
{
    public class DsReplayRequest
    {
        public int Id { get; set; }
        public List<SortOrder> sortOrders { get; set; }
        public string Race1 { get; set; }
        public string Race2 { get; set; }
        public string Race3 { get; set; }
        public string Opp1 { get; set; }
        public string Opp2 { get; set; }
        public string Opp3 { get; set; }
        public string Playername { get; set; }
        public string PlayerRace { get; set; }
        public List<string> Races { get; set; } = new List<string>();
        public List<string> Opponents { get; set; } = new List<string>();
        public List<string> Players { get; set; } = new List<string>();
        public bool DefaultFilter { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public DsFilter Filter { get; set; }
    }

    public class SortOrder
    {
        public string Sort { get; set; }
        public bool Order { get; set; }
    }
}
