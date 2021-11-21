using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;

namespace sc2dsstats.db.Services;

// app only
public class PlayerNameService
{
    public static async Task<List<PlayerNameResponse>> GetPlayers(sc2dsstatsContext context)
    {
        var request = from r in context.Dsreplays
                      from p in r.Dsplayers
                      group p by p.Name into g
                      select new PlayerNameResponse()
                      {
                          Name = g.Key,
                          Games = g.Count(),
                          Wins = g.Count(c => c.Win)
                      };
        return await request
            .AsNoTracking()
            .OrderByDescending(o => o.Games)
            .ToListAsync();
    }

    public static async Task<PlayerNameStatsResponse?> GetPlayerStats(sc2dsstatsContext context, string name, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return null;
        var player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Name == name, token);
        if (token.IsCancellationRequested)
            return null;
        var appplayer = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.AppId != Guid.Empty, token);


        var plrepsTeam = from r in context.Dsreplays
                         from p in r.Dsplayers
                         from a in r.Dsplayers
                         where a.PlayerName == player
                         where p.PlayerName == appplayer
                         where a.Team == p.Team
                         group a by a.Win into g
                         select new
                         {
                             Win = g.Key,
                             Games = g.Count(),
                         };
        var plrepsOpp = from r in context.Dsreplays
                        from p in r.Dsplayers
                        from a in r.Dsplayers
                        where a.PlayerName == player
                        where p.PlayerName == appplayer
                        where a.Team != p.Team
                        group a by a.Win into g
                        select new
                        {
                            Win = g.Key,
                            Games = g.Count(),
                        };

        if (token.IsCancellationRequested)
            return null;
        var teamStats = await plrepsTeam.AsNoTracking().ToListAsync(token);
        if (token.IsCancellationRequested)
            return null;
        var oppStats = await plrepsOpp.AsNoTracking().ToListAsync(token);

        return new PlayerNameStatsResponse()
        {
            TeamGames = teamStats.Sum(s => s.Games),
            TeamWins = teamStats.Where(x => x.Win).Sum(s => s.Games),
            OppGames = oppStats.Sum(s => s.Games),
            OppWins = oppStats.Where(x => x.Win).Sum(s => s.Games)
        };
    }
}
