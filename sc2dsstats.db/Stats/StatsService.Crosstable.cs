using sc2dsstats._2022.Shared;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static DsResponse GetCrosstable(DsRequest request, List<CmdrStats> cmdrstats)
        {
            var stats = GetTimeStats(request, cmdrstats);

            List<CmdrStats> laststats = new List<CmdrStats>(stats);
            if (stats.Any())
            {
                var last = stats.OrderByDescending(o => o.year).ThenByDescending(o => o.month).Last();
                laststats.RemoveAll(x => x.year == last.year && x.month == last.month);
                if (DateTime.Today.Day < 16)
                {
                    if (last.month > 1)
                        laststats.RemoveAll(x => x.year == last.year && x.month == last.month - 1);
                    else
                        laststats.RemoveAll(x => x.year == last.year - 1 && x.month == 12);
                }
            }

            List<DsResponseItem> lastitems = new List<DsResponseItem>();
            if (laststats.Any())
            {
                lastitems = laststats.GroupBy(g => new { cmdr = g.RACE, opp = g.OPPRACE }).Select(s => new DsResponseItem()
                {
                    Label = $"{(DSData.Commander)s.Key.cmdr} vs {(DSData.Commander)s.Key.opp}",
                    Count = s.Sum(s => s.count),
                    Wins = s.Sum(s => s.wins),
                }).ToList();
            }

            var items = stats.GroupBy(g => new { cmdr = g.RACE, opp = g.OPPRACE }).Select(s => new CrosstableResponseItem()
            {
                Label = $"{(DSData.Commander)s.Key.cmdr} vs {(DSData.Commander)s.Key.opp}",
                Count = s.Sum(s => s.count),
                Wins = s.Sum(s => s.wins),
                OldCount = lastitems.FirstOrDefault(f => f.Label == $"{(DSData.Commander)s.Key.cmdr} vs {(DSData.Commander)s.Key.opp}") != null ? lastitems.FirstOrDefault(f => f.Label == $"{(DSData.Commander)s.Key.cmdr} vs {(DSData.Commander)s.Key.opp}").Count : 0,
                OldWins = lastitems.FirstOrDefault(f => f.Label == $"{(DSData.Commander)s.Key.cmdr} vs {(DSData.Commander)s.Key.opp}") != null ? lastitems.FirstOrDefault(f => f.Label == $"{(DSData.Commander)s.Key.cmdr} vs {(DSData.Commander)s.Key.opp}").Wins : 0,
            }).OrderBy(o => o.Label).ToList();

            return new CrosstableResponse()
            {
                Interest = "CrossTable",
                Items = items
            };
        }
    }
}
