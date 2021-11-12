using System.Linq;

namespace sc2dsstats._2022.Shared
{
    public class DsPlayerStats
    {
        public DsResponse Winrate { get; set; }
        public DsResponse Matchups { get; set; }

        public DsResponseItem mpItem => Winrate != null && Winrate.Items.Any() ? Winrate.Items.OrderByDescending(o => o.Count).First() : null;
        public DsResponseItem lpItem => Winrate != null && Winrate.Items.Any() ? Winrate.Items.OrderBy(o => o.Count).First() : null;
        public DsResponseItem bItem => Winrate != null && Winrate.Items.Any() ? Winrate.Items.OrderByDescending(o => o.Winrate).First() : null;
        public DsResponseItem wItem => Winrate != null && Winrate.Items.Any() ? Winrate.Items.OrderBy(o => o.Winrate).First() : null;
        public DsResponseItem bmItem => Matchups != null && Matchups.Items.Any() ? Matchups.Items.OrderByDescending(o => o.Winrate).First() : null;
        public DsResponseItem wmItem => Matchups != null && Matchups.Items.Any() ? Matchups.Items.OrderBy(o => o.Winrate).First() : null;
    }
}
