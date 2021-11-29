using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;

namespace sc2dsstats.db.Services
{
    public static class PlayerService
    {
        public static async Task<DsPlayerName> GetPlayerName(sc2dsstatsContext context, DsUploadRequest request)
        {
            var playerName = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.AppId == request.AppId);
            if (playerName == null)
            {
                playerName = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Hash == request.Hash);
            }
            if (playerName == null)
            {
                var restPlayer = await context.DSRestPlayers.FirstOrDefaultAsync(f => f.Name == request.Hash);
                playerName = new DsPlayerName()
                {
                    AppId = request.AppId,
                    DbId = Guid.NewGuid(),
                    Hash = request.Hash,
                    Name = String.Empty,
                    AppVersion = request.Version,
                    LatestReplay = restPlayer == null ? new DateTime(2018, 1, 1) : restPlayer.LastRep
                };
                context.DsPlayerNames.Add(playerName);
            }
            // TODO DEBUG
            // else if (playerName.AppId == new Guid())
            // {
            playerName.AppId = request.AppId;
            // }
            playerName.TotlaReplays = request.Total;
            playerName.LatestUpload = DateTime.UtcNow;
            playerName.AppVersion = request.Version;
            await context.SaveChangesAsync();

            return playerName;
        }

        public static async Task<DsPlayerName> GetPlayerName(sc2dsstatsContext context, DsUploadInfo request)
        {
            var playerName = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Hash == request.Name);
            if (playerName == null)
            {
                var restPlayer = await context.DSRestPlayers.FirstOrDefaultAsync(f => f.Name == request.Name);
                playerName = new DsPlayerName()
                {
                    AppId = new Guid(),
                    DbId = Guid.NewGuid(),
                    Hash = request.Name,
                    Name = String.Empty,
                    AppVersion = request.Version,
                    LatestReplay = restPlayer == null ? new DateTime(2018, 1, 1) : restPlayer.LastRep
                };
                context.DsPlayerNames.Add(playerName);
                await context.SaveChangesAsync();
            }
            return playerName;
        }

        public static async Task GetPlayerStats(sc2dsstatsContext context, List<string> playerNames)
        {
            var replays = ReplayFilter.DefaultFilter(context);
            var cmdrs = from r in replays
                        from p in r.Dsplayers
                        where playerNames.Contains(p.Name)
                        group p by p.Race into g
                        select new
                        {
                            cmdr = g.Key,
                            count = g.Count()
                        };
            var lcmdrs = await cmdrs.ToListAsync();
            if (lcmdrs.Any())
            {
                var count = lcmdrs.Sum(s => s.count);
                var mostplayed = lcmdrs.OrderByDescending(o => o.count).First();
                var leastplayed = lcmdrs.OrderByDescending(o => o.count).Last();
                Console.WriteLine($"Most played Commander is {mostplayed.cmdr} ({(mostplayed.count * 100 / (double)count).ToString("N2")})");
                Console.WriteLine($"Least played Commander is {leastplayed.cmdr} ({(leastplayed.count * 100 / (double)count).ToString("N2")})");
            }

            var wrs = from r in replays
                      from p in r.Dsplayers
                      where playerNames.Contains(p.Name)
                      group p by p.Race into g
                      select new
                      {
                          cmdr = g.Key,
                          count = g.Count(),
                          wins = g.Count(s => s.Win)
                      };
            var lwrs = await wrs.ToListAsync();
            if (lwrs.Any())
            {
                var bestcmdr = lwrs.OrderByDescending(o => o.wins * 100 / (double)o.count).First();
                var worstcmdr = lwrs.OrderByDescending(o => o.wins * 100 / (double)o.count).Last();
                Console.WriteLine($"Best cmdr is {bestcmdr.cmdr} ({(bestcmdr.wins * 100 / (double)bestcmdr.count).ToString("N2")})");
                Console.WriteLine($"Worst cmdr is {worstcmdr.cmdr} ({(worstcmdr.wins * 100 / (double)worstcmdr.count).ToString("N2")})");
            }

            var matchups = from r in replays
                           from p in r.Dsplayers
                           where playerNames.Contains(p.Name)
                           group p by new { cmdr = p.Race, opp = p.Opprace } into g
                           select new
                           {
                               matchup = g.Key,
                               count = g.Count(),
                               wins = g.Count(s => s.Win)
                           };
            var lmatchups = await matchups.ToListAsync();
            if (lmatchups.Any())
            {
                var bestmatchup = lmatchups.OrderByDescending(o => o.wins * 100 / (double)o.count).First();
                var worstmatchup = lmatchups.OrderByDescending(o => o.wins * 100 / (double)o.count).Last();
                Console.WriteLine($"Best matchup is {bestmatchup.matchup.cmdr} vs {bestmatchup.matchup.opp} ({(bestmatchup.wins * 100 / (double)bestmatchup.count).ToString("N2")})");
                Console.WriteLine($"Worst matchup is {worstmatchup.matchup.cmdr} vs {worstmatchup.matchup.opp} ({(worstmatchup.wins * 100 / (double)worstmatchup.count).ToString("N2")})");
            }

            var poss = from r in replays
                       from p in r.Dsplayers
                       where playerNames.Contains(p.Name)
                       group p by p.Pos into g
                       select new
                       {
                           pos = g.Key,
                           count = g.Count(),
                           wins = g.Count(s => s.Win)
                       };
            var lposs = await poss.ToListAsync();
            if (lposs.Any())
            {
                foreach (var pos in lposs.OrderBy(o => o.pos))
                {
                    Console.WriteLine($"Pos: {pos.pos} => {(pos.wins * 100 / (double)pos.count).ToString("N2")}");
                }
            }
        }
    }
}
