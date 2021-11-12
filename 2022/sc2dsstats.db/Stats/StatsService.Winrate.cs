using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using sc2dsstats.db.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static DsResponse GetWinrate(DsRequest request, List<CmdrStats> cmdrstats)
        {
            var stats = GetTimeStats(request, cmdrstats);

            if (request.Interest != "ALL")
            {
                stats = stats.Where(x => x.RACE == (byte)DSData.GetCommander(request.Interest)).ToList();
            }

            List<DsResponseItem> data = request.Interest == "ALL"
                ? stats.GroupBy(g => g.RACE).Select(s => new DsResponseItem
                {
                    Label = ((DSData.Commander)s.Key).ToString(),
                    Count = s.Sum(c => c.count),
                    Wins = s.Sum(c => c.wins),
                }).ToList()
                : stats.GroupBy(g => g.OPPRACE).Select(s => new DsResponseItem
                {
                    Label = ((DSData.Commander)s.Key).ToString(),
                    Count = s.Sum(c => c.count),
                    Wins = s.Sum(c => c.wins),
                }).ToList();

            int tcount = data.Sum(s => s.Count);
            return new DsResponse()
            {
                Interest = request.Interest,
                Count = request.Player ? tcount : tcount / 6,
                AvgDuration = tcount == 0 ? 0 : (int)(stats.Sum(s => s.duration) / tcount),
                Items = data
            };
        }

        public static async Task<DsResponse> GetCustomWinrate(DsRequest request, sc2dsstatsContext context)
        {
            var replays = ReplayFilter.Filter(context, request);

            var responses = (request.Player, request.Interest == "ALL") switch
            {
                (false, true) => from r in replays
                                 from p in r.Dsplayers
                                 group new { r, p } by new { race = p.Race } into g
                                 select new DsResponseItem()
                                 {
                                     Label = ((DSData.Commander)g.Key.race).ToString(),
                                     Count = g.Count(),
                                     Wins = g.Count(c => c.p.Win),
                                     duration = g.Sum(s => s.r.Duration),
                                     Replays = g.Select(s => s.r.Id).Distinct().Count()
                                 },
                (false, false) => from r in replays
                                  from p in r.Dsplayers
                                  where p.Race == (byte)DSData.GetCommander(request.Interest)
                                  group new { r, p } by new { race = p.Opprace } into g
                                  select new DsResponseItem()
                                  {
                                      Label = ((DSData.Commander)g.Key.race).ToString(),
                                      Count = g.Count(),
                                      Wins = g.Count(c => c.p.Win),
                                      duration = g.Sum(s => s.r.Duration),
                                      Replays = g.Select(s => s.r.Id).Distinct().Count()
                                  },
                (true, true) => from r in replays
                                from p in r.Dsplayers
                                where p.isPlayer
                                group new { r, p } by new { race = p.Race } into g
                                select new DsResponseItem()
                                {
                                    Label = ((DSData.Commander)g.Key.race).ToString(),
                                    Count = g.Count(),
                                    Wins = g.Count(c => c.p.Win),
                                    duration = g.Sum(s => s.r.Duration),
                                    Replays = g.Select(s => s.r.Id).Distinct().Count()
                                },
                (true, false) => from r in replays
                                 from p in r.Dsplayers
                                 where p.isPlayer && p.Race == (byte)DSData.GetCommander(request.Interest)
                                 group new { r, p } by new { race = p.Opprace } into g
                                 select new DsResponseItem()
                                 {
                                     Label = ((DSData.Commander)g.Key.race).ToString(),
                                     Count = g.Count(),
                                     Wins = g.Count(c => c.p.Win),
                                     duration = g.Sum(s => s.r.Duration),
                                     Replays = g.Select(s => s.r.Id).Distinct().Count()
                                 }
            };

            var items = await responses.ToListAsync();
            // items = items.Where(x => DSData.cmdrs.Contains(x.Label)).ToList();

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
