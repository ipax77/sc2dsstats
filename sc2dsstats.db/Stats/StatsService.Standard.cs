using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using System.Text.RegularExpressions;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static async Task<DsResponse> GetStandardTeamWinrate(DsRequest request, sc2dsstatsContext context)
        {
            var replays = context.Dsreplays
                .Include(i => i.Dsplayers)
                .AsNoTracking()
                .Where(x =>
                    x.Gamemode == (byte)DSData.Gamemode.Standard
                    && x.Gametime > request.StartTime
                    && x.Duration > 300
                    && x.Maxleaver < 90
                    && x.Playercount == 6
                );
            if (request.EndTime != DateTime.Today)
            {
                replays = replays.Where(x => x.Gametime < request.EndTime);
            }

            byte[] races = null;
            if (!String.IsNullOrEmpty(request.Interest) && request.Interest != "ALL")
            {
                string[] iSplit = Regex.Split(request.Interest, @"(?<!^)(?=[A-Z])");
                if (iSplit.Length == 3)
                    races = iSplit.Select(s => (byte)DSData.GetCommander(s)).ToArray();
            }

            List<DsResponseItem> teamResponses = new List<DsResponseItem>();
            for (int p1 = 1; p1 < 4; p1++)
            {
                for (int p2 = 1; p2 < 4; p2++)
                {
                    for (int p3 = 1; p3 < 4; p3++)
                    {
                        var team1Replays = races == null ?
                         replays.Where(x =>
                         (x.Dsplayers.Where(s => s.Realpos == 1 && s.Race == p1).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 2 && s.Race == p2).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 3 && s.Race == p3).Any())
                        )
                        : replays.Where(x =>
                         (x.Dsplayers.Where(s => s.Realpos == 1 && s.Race == p1).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 2 && s.Race == p2).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 3 && s.Race == p3).Any())
                         &&
                        (x.Dsplayers.Where(s => s.Realpos == 4 && s.Race == races[0]).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 5 && s.Race == races[1]).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 6 && s.Race == races[2]).Any())
                        );

                        var team2Replays = races == null ?
                         replays.Where(x =>
                         (x.Dsplayers.Where(s => s.Realpos == 4 && s.Race == p1).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 5 && s.Race == p2).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 6 && s.Race == p3).Any())
                        )
                        : replays.Where(x =>
                         (x.Dsplayers.Where(s => s.Realpos == 4 && s.Race == p1).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 5 && s.Race == p2).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 6 && s.Race == p3).Any())
                         &&
                        (x.Dsplayers.Where(s => s.Realpos == 1 && s.Race == races[0]).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 2 && s.Race == races[1]).Any()
                         && x.Dsplayers.Where(s => s.Realpos == 3 && s.Race == races[2]).Any())
                        );

                        var team1Results = from r in team1Replays
                                           group r by r.Winner into g
                                           select new
                                           {
                                               Winner = g.Key,
                                               Count = g.Count(),
                                               duration = g.Sum(s => s.Duration),
                                           };
                        var team2Results = from r in team2Replays
                                           group r by r.Winner into g
                                           select new
                                           {
                                               Winner = g.Key,
                                               Count = g.Select(s => s.Id).Distinct().Count(),
                                               duration = g.Sum(s => s.Duration),
                                           };

                        var t1 = await team1Results.AsNoTracking().ToListAsync();
                        var t2 = await team2Results.AsNoTracking().ToListAsync();
                        int count = t1.Sum(s => s.Count) + t2.Sum(s => s.Count);
                        if (count > 0)
                        {
                            teamResponses.Add(new DsResponseItem()
                            {
                                Label = ((DSData.Commander)p1).ToString() + ((DSData.Commander)p2).ToString() + ((DSData.Commander)p3).ToString(),
                                Count = count,
                                Wins = t1.Where(x => x.Winner == 0).Sum(s => s.Count) + t2.Where(x => x.Winner == 1).Sum(s => s.Count),
                                duration = t1.Sum(s => s.duration) + t2.Sum(s => s.duration),
                                Replays = count,
                            });
                        }
                    }
                }
            }

            int tcount = teamResponses.Sum(s => s.Count);
            return new DsResponse()
            {
                Interest = request.Interest,
                Count = tcount,
                AvgDuration = tcount == 0 ? 0 : (int)(teamResponses.Sum(s => s.duration) / tcount),
                Items = teamResponses
            };
        }

        public static async Task<DsResponse> GetStandardWinrate(DsRequest request, sc2dsstatsContext context)
        {
            var replays = context.Dsreplays.Where(x =>
                x.Gamemode == (byte)DSData.Gamemode.Standard
                && x.Gametime > request.StartTime
            ).AsNoTracking();

            if (request.EndTime < DateTime.Today)
            {
                replays = replays.Where(x => x.Gametime < request.EndTime);
            }


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
            items = items.Where(x => DSData.cmdrs.Contains(x.Label)).ToList();

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
