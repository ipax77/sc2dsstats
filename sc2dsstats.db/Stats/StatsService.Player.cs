using Microsoft.EntityFrameworkCore;
using sc2dsstats.shared;
using sc2dsstats.db.Services;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static async Task GetTeammateStats(sc2dsstatsContext context)
        {
            //select p.name, p.Team, count(*) as c from DSReplays as r
            //inner join DSPlayers as p on p.DSReplayID = r.ID
            //where 0 = (select t.Team from DSPlayers as t where t.DSReplayID = r.ID and t.name = 'PAX')
            //group by p.name, p.Team
            //order by c desc
        }

        public static async Task<DsPlayerStats> GetPlayerStats(sc2dsstatsContext context, List<string> playerNames)
        {

            // Console.WriteLine($"Most played Commander is {mostplayed.cmdr} ({(mostplayed.count * 100 / (double)count).ToString("N2")})");
            // Console.WriteLine($"Least played Commander is {leastplayed.cmdr} ({(leastplayed.count * 100 / (double)count).ToString("N2")})");
            var replays = ReplayFilter.DefaultFilter(context);

            var stats = from r in replays
                        from p in r.Dsplayers
                        where playerNames.Contains(p.Name)
                        group p by p.Race into g
                        select new DsResponseItem
                        {
                            Label = ((DSData.Commander)g.Key).ToString(),
                            Count = g.Count(),
                            Wins = g.Count(s => s.Win)
                        };
            var items = await stats.ToListAsync();
            items = items.Where(x => DSData.cmdrs.Contains(x.Label)).OrderByDescending(o => o.Winrate).ToList();

            // Console.WriteLine($"Best cmdr is {bestcmdr.cmdr} ({(bestcmdr.wins * 100 / (double)bestcmdr.count).ToString("N2")})");
            // Console.WriteLine($"Worst cmdr is {worstcmdr.cmdr} ({(worstcmdr.wins * 100 / (double)worstcmdr.count).ToString("N2")})");

            var matchups = from r in replays
                           from p in r.Dsplayers
                           where playerNames.Contains(p.Name) && Enum.GetValues<DSData.Commander>().Select(s => (byte)s).Contains(p.Race) && Enum.GetValues<DSData.Commander>().Select(s => (byte)s).Contains(p.Opprace)
                           group p by new { cmdr = p.Race, opp = p.Opprace } into g
                           select new DsResponseItem
                           {
                               Label = $"{((DSData.Commander)g.Key.cmdr).ToString()} vs {((DSData.Commander)g.Key.opp).ToString()}",
                               Count = g.Count(),
                               Wins = g.Count(s => s.Win)
                           };
            var lmatchups = await matchups.ToListAsync();

            // Console.WriteLine($"Best matchup is {bestmatchup.matchup.cmdr} vs {bestmatchup.matchup.opp} ({(bestmatchup.wins * 100 / (double)bestmatchup.count).ToString("N2")})");
            // Console.WriteLine($"Worst matchup is {worstmatchup.matchup.cmdr} vs {worstmatchup.matchup.opp} ({(worstmatchup.wins * 100 / (double)worstmatchup.count).ToString("N2")})");

            return new DsPlayerStats()
            {
                Winrate = new DsResponse()
                {
                    Count = items.Sum(s => s.Count),
                    Items = items
                },
                Matchups = new DsResponse()
                {
                    Count = lmatchups.Sum(s => s.Count),
                    Items = lmatchups
                }
            };

            // var poss = from r in replays
            //            from p in r.Dsplayers
            //            where playerNames.Contains(p.Name) && p.Opprace != null
            //            group p by p.Pos into g
            //            select new
            //            {
            //                pos = g.Key,
            //                count = g.Count(),
            //                wins = g.Count(s => s.Win)
            //            };
            // var lposs = await poss.ToListAsync();
            // if (lposs.Any())
            // {
            //     foreach (var pos in lposs.OrderBy(o => o.pos))
            //     {
            //         Console.WriteLine($"Pos: {pos.pos} => {(pos.wins * 100 / (double)pos.count).ToString("N2")}");
            //     }
            // }
        }

        public static async Task<DsPlayerStats> GetPlayerStats(sc2dsstatsContext context, string playerName)
        {

            var replays = ReplayFilter.DefaultFilter(context);

            Guid guid;
            DsPlayerName player;
            if (Guid.TryParse(playerName, out guid))
            {
                player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.DbId.Equals(guid));
            }
            else
            {
                player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Name == playerName);
            }
            if (player == null)
            {
                return null;
            }

            var stats = from r in replays
                        from p in r.Dsplayers
                        where p.PlayerName == player
                        group p by p.Race into g
                        select new DsResponseItem
                        {
                            Label = ((DSData.Commander)g.Key).ToString(),
                            Count = g.Count(),
                            Wins = g.Count(s => s.Win)
                        };
            var items = await stats.ToListAsync();
            items = items.Where(x => DSData.cmdrs.Contains(x.Label)).OrderByDescending(o => o.Winrate).ToList();


            var matchups = from r in replays
                           from p in r.Dsplayers
                           where p.PlayerName == player && Enum.GetValues<DSData.Commander>().Select(s => (byte)s).Contains(p.Race) && Enum.GetValues<DSData.Commander>().Select(s => (byte)s).Contains(p.Opprace)
                           group p by new { cmdr = p.Race, opp = p.Opprace } into g
                           select new DsResponseItem
                           {
                               Label = $"{((DSData.Commander)g.Key.cmdr).ToString()} vs {((DSData.Commander)g.Key.opp).ToString()}",
                               Count = g.Count(),
                               Wins = g.Count(s => s.Win)
                           };
            var lmatchups = await matchups.ToListAsync();

            return new DsPlayerStats()
            {
                Winrate = new DsResponse()
                {
                    Count = items.Sum(s => s.Count),
                    Items = items
                },
                Matchups = new DsResponse()
                {
                    Count = lmatchups.Sum(s => s.Count),
                    Items = lmatchups
                }
            };

        }
    }
}
