using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using static sc2dsstats._2022.Shared.DSData;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static async Task<List<CmdrStats>> GetStats(sc2dsstatsContext context, bool player)
        {
            var stats = player
                ? from r in context.Dsreplays
                  from p in r.Dsplayers
                  where r.DefaultFilter && p.isPlayer
                  group new { r, p } by new { year = r.Gametime.Year, month = r.Gametime.Month, race = p.Race, opprace = p.Opprace } into g
                  select new CmdrStats()
                  {
                      year = g.Key.year,
                      month = g.Key.month,
                      RACE = g.Key.race,
                      OPPRACE = g.Key.opprace,
                      count = g.Count(),
                      wins = g.Count(c => c.p.Win),
                      mvp = g.Count(c => c.p.Killsum == c.r.Maxkillsum),
                      army = g.Sum(s => s.p.Army),
                      kills = g.Sum(s => s.p.Killsum),
                      duration = g.Sum(s => s.r.Duration),
                  }
                : from r in context.Dsreplays
                  from p in r.Dsplayers
                  where r.DefaultFilter
                  group new { r, p } by new { year = r.Gametime.Year, month = r.Gametime.Month, race = p.Race, opprace = p.Opprace } into g
                  select new CmdrStats()
                  {
                      year = g.Key.year,
                      month = g.Key.month,
                      RACE = g.Key.race,
                      OPPRACE = g.Key.opprace,
                      count = g.Count(),
                      wins = g.Count(c => c.p.Win),
                      mvp = g.Count(c => c.p.Killsum == c.r.Maxkillsum),
                      army = g.Sum(s => s.p.Army),
                      kills = g.Sum(s => s.p.Killsum),
                      duration = g.Sum(s => s.r.Duration),
                  };
            var cmdrstats = await stats.ToListAsync();

            cmdrstats = cmdrstats.Where(x => DSData.GetCommanders.Select(s => (byte)s).Contains(x.RACE)).ToList();
            cmdrstats = cmdrstats.Where(x => DSData.GetCommanders.Select(s => (byte)s).Contains(x.OPPRACE)).ToList();

            return cmdrstats;
        }

        public static async Task<double> GetLeaver(sc2dsstatsContext context, DsRequest request)
        {
            var replays = GetCountReplays(context, request);
            var bab = await replays.ToListAsync();

            var leaver = from r in replays
                         group r by r.Maxleaver > 89 into g
                         select new
                         {
                             Leaver = g.Key,
                             Count = g.Count()
                         };
            var lleaver = await leaver.ToListAsync();
            if (lleaver.Any() && lleaver.First().Count > 0)
                return Math.Round(lleaver.Last().Count / (double)lleaver.First().Count * 100, 2);
            else
                return 0;
        }

        public static async Task<double> GetQuits(sc2dsstatsContext context, DsRequest request)
        {
            var replays = GetCountReplays(context, request);



            var quits = from r in replays
                        group r by r.Winner into g
                        select new
                        {
                            Winner = g.Key,
                            Count = g.Count()
                        };
            var lquits = await quits.ToListAsync();
            if (lquits.Any())
            {
                double sum = (double)lquits.Sum(s => s.Count);
                if (sum > 0)
                    return Math.Round(lquits.Where(x => x.Winner == -1).Count() * 100 / sum, 2);
                else
                    return 0;
            }
            else
                return 0;
        }

        public static async Task<DsCountResponse> GetCount(sc2dsstatsContext context, DsRequest request, bool details = false)
        {
            var replays = GetCountReplays(context, request);

            var count = from r in replays
                        group r by r.DefaultFilter into g
                        select new
                        {
                            DefaultFilter = g.Key,
                            Count = g.Count()
                        };
            var lcount = await count.ToListAsync();

            if (lcount.Any())
            {
                var response = new DsCountResponse()
                {
                    TotalCount = lcount.Sum(s => s.Count),
                    Leaver = await GetLeaver(context, request),
                    Quits = await GetQuits(context, request),
                    FilteredCount = lcount.FirstOrDefault(f => f.DefaultFilter) != null ? lcount.First(f => f.DefaultFilter == true).Count : 0
                };

                if (response.FilteredCount == 0 && response.TotalCount > 0)
                {
                    response.FilteredCount = response.TotalCount;
                    response.TotalCount = 0;
                }

                if (details)
                {
                    response.StdCount = await replays.Where(x => x.Gamemode == (int)Gamemode.Standard).CountAsync();
                    response.CmdrCount = response.TotalCount - response.StdCount;
                }
                return response;
            }
            else
            {
                return new DsCountResponse();
            }
        }

        private static IQueryable<Dsreplay> GetCountReplays(sc2dsstatsContext context, DsRequest request)
        {
            var replays = context.Dsreplays.Where(x => x.Gametime > request.StartTime);

            if (request.Filter == null)
            {
                replays = replays.Where(x => new List<int>() { (int)Gamemode.Commanders, (int)Gamemode.CommandersHeroic }.Contains(x.Gamemode));
            }
            else if (request.Filter.GameModes.Any())
            {
                replays = replays.Where(x => request.Filter.GameModes.Contains(x.Gamemode));
            }

            if (request.EndTime != DateTime.Today)
                replays = replays.Where(x => x.Gametime <= request.EndTime);

            if (!String.IsNullOrEmpty(request.Interest) && !request.Interest.Equals("ALL"))
            {
                replays = replays.Include(i => i.Dsplayers);
                if (request.Player)
                {
                    replays = from r in replays
                              from p in r.Dsplayers
                              where p.isPlayer && p.Race == (byte)DSData.GetCommander(request.Interest)
                              select r;
                }
                else
                {
                    replays = from r in replays
                              from p in r.Dsplayers
                              where p.Race == (byte)DSData.GetCommander(request.Interest)
                              select r;
                }
                replays = replays.Distinct();
            }

            return replays;
        }

        public static List<CmdrStats> GetTimeStats(DsRequest request, List<CmdrStats> stats)
        {
            return request.Timespan switch
            {
                "This Month" => stats.Where(x => x.year == DateTime.Today.Year && x.month == DateTime.Today.Month).ToList(),
                "Last Month" => stats.Where(x => x.year == DateTime.Today.AddMonths(-1).Year && x.month == DateTime.Today.AddMonths(-1).Month).ToList(),
                "This Year" => stats.Where(x => x.year == DateTime.Today.Year).ToList(),
                "Last Year" => stats.Where(x => x.year == DateTime.Today.AddYears(-1).Year).ToList(),
                "Last Two Years" => stats.Where(x => x.year == DateTime.Today.Year || x.year == DateTime.Today.AddYears(-1).Year).ToList(),
                "ALL" => stats,
                "Patch 2.60" => GetTimeStats(new DateTime(2020, 07, 28, 5, 23, 0), DateTime.Today.AddDays(1), stats),
                "Custom" => GetTimeStats(request.StartTime, request.EndTime, stats),
                _ => stats
            };
        }

        private static List<CmdrStats> GetTimeStats(DateTime from, DateTime to, List<CmdrStats> stats)
        {
            if (from > to)
                return new List<CmdrStats>();

            List<int> years = new List<int>();
            List<int> frommonths = new List<int>();
            if (from.Year != to.Year)
            {
                DateTime stepTime = new DateTime(from.Year, 1, 1);

                if (from.Month != 1)
                {
                    DateTime fstepTime = new DateTime(from.Year, from.Month, 1);
                    do
                    {
                        frommonths.Add(fstepTime.Month);
                        fstepTime = fstepTime.AddMonths(1);
                    } while (fstepTime.Year == from.Year);
                    stepTime = stepTime.AddYears(1);
                }

                if (stepTime.Year != to.Year)
                {
                    do
                    {
                        years.Add(stepTime.Year);
                        stepTime = stepTime.AddYears(1);
                    } while (stepTime.Year < to.Year - 1);
                }
            }

            List<int> tomonths = new List<int>();
            DateTime mstepTime = new DateTime(to.Year, to.Month, 1);
            do
            {
                tomonths.Add(mstepTime.Month);
                mstepTime = mstepTime.AddMonths(-1);
            } while (mstepTime.Year == to.Year);

            return stats.Where(x =>
                   (x.year == from.Year && frommonths.Contains(x.month))
                || years.Contains(x.year)
                || (x.year == to.Year && tomonths.Contains(x.month))).ToList();
        }


    }
}
