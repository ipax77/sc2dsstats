using Microsoft.EntityFrameworkCore;
using sc2dsstats.shared;
using sc2dsstats.db.Services;
using static sc2dsstats.shared.DSData;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static DsResponse GetDps(DsRequest request, List<CmdrStats> cmdrstats)
        {
            var stats = GetTimeStats(request, cmdrstats);

            if (request.Interest != "ALL")
            {
                stats = stats.Where(x => x.RACE == (byte)DSData.GetCommander(request.Interest)).ToList();
            }

            List<DsResponseItem> items = request.Interest == "ALL"
                ? stats.GroupBy(g => g.RACE).Select(s => new DsResponseItem
                {
                    Label = ((Commander)s.Key).ToString(),
                    Count = (int)s.Sum(c => c.army),
                    Wins = (int)s.Sum(c => c.kills),
                }).ToList()
                : stats.GroupBy(g => g.OPPRACE).Select(s => new DsResponseItem
                {
                    Label = ((Commander)s.Key).ToString(),
                    Count = (int)s.Sum(c => c.army),
                    Wins = (int)s.Sum(c => c.kills),
                }).ToList();
            items.ForEach(f => { f.Count = (int)(((double)f.Count / (double)f.Wins) * 10000); f.Wins = 10000; });
            int tcount = items.Sum(s => s.Count);
            return new DsResponse()
            {
                Interest = request.Interest,
                Count = request.Player ? tcount : tcount / 6,
                AvgDuration = tcount == 0 ? 0 : (int)(stats.Sum(s => s.duration) / tcount),
                Items = items
            };
        }

        public static async Task<DsResponse> GetCustomDps(DsRequest request, sc2dsstatsContext context)
        {
            var replays = ReplayFilter.Filter(context, request);

            var responses = (request.Player, request.Interest == "ALL") switch
            {
                (false, true) => from r in replays
                                 from p in r.Dsplayers
                                 group new { r, p } by new { race = p.Race } into g
                                 select new DsResponseItem()
                                 {
                                     Label = ((Commander)g.Key.race).ToString(),
                                     Count = g.Sum(c => c.p.Army),
                                     Wins = g.Sum(c => c.p.Killsum),
                                     duration = g.Sum(s => s.r.Duration)
                                 },
                (false, false) => from r in replays
                                  from p in r.Dsplayers
                                  where p.Race == (byte)DSData.GetCommander(request.Interest)
                                  group new { r, p } by new { race = p.Opprace } into g
                                  select new DsResponseItem()
                                  {
                                      Label = ((Commander)g.Key.race).ToString(),
                                      Count = g.Sum(c => c.p.Army),
                                      Wins = g.Sum(c => c.p.Killsum),
                                      duration = g.Sum(s => s.r.Duration)
                                  },
                (true, true) => from r in replays
                                from p in r.Dsplayers
                                where p.isPlayer
                                group new { r, p } by new { race = p.Race } into g
                                select new DsResponseItem()
                                {
                                    Label = ((Commander)g.Key.race).ToString(),
                                    Count = g.Sum(c => c.p.Army),
                                    Wins = g.Sum(c => c.p.Killsum),
                                    duration = g.Sum(s => s.r.Duration)
                                },
                (true, false) => from r in replays
                                 from p in r.Dsplayers
                                 where p.isPlayer && p.Race == (byte)DSData.GetCommander(request.Interest)
                                 group new { r, p } by new { race = p.Opprace } into g
                                 select new DsResponseItem()
                                 {
                                     Label = ((Commander)g.Key.race).ToString(),
                                     Count = g.Sum(c => c.p.Army),
                                     Wins = g.Sum(c => c.p.Killsum),
                                     duration = g.Sum(s => s.r.Duration)
                                 }
            };

            var items = await responses.ToListAsync();
            items = items.Where(x => Enum.GetNames<DSData.Commander>().Contains(x.Label)).ToList();
            items.ForEach(f => { f.Count = (int)(((double)f.Count / (double)f.Wins) * 10000); f.Wins = 10000; });
            int tcount = items.Sum(s => s.Count);
            return new DsResponse()
            {
                Interest = request.Interest,
                Count = tcount / 6,
                AvgDuration = tcount == 0 ? 0 : (int)(items.Sum(s => s.duration) / tcount),
                Items = items
            };
        }
    }
}
