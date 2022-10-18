using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pax.dsstats.dbng.Services;
public partial class StatsService
{
    private async Task<double> GetLeaver(StatsRequest request)
    {
        var replays = GetCountReplays(request);

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

    private async Task<double> GetQuits(StatsRequest request)
    {
        var replays = GetCountReplays(request);

        var quits = from r in replays
                    group r by r.WinnerTeam into g
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

    private async Task<(int, int)> GetCount(StatsRequest request, bool details = false)
    {
        var replays = GetCountReplays(request);

        var count = from r in replays
                    group r by r.DefaultFilter into g
                    select new
                    {
                        DefaultFilter = g.Key,
                        Count = g.Count()
                    };
        var lcount = await count.ToListAsync();
        return (lcount.First(f => !f.DefaultFilter).Count, lcount.First(f => f.DefaultFilter).Count);
    }

    private IQueryable<Replay> GetCountReplays(StatsRequest request)
    {
        var replays = (request.Interest == Commander.None && !request.PlayerNames.Any()) ?
                context.Replays
                .Where(x => x.GameTime > request.StartTime)
                .AsNoTracking()
                : context.Replays
                .Include(i => i.Players)
                .Where(x => x.GameTime > request.StartTime)
                .AsNoTracking();

        if (request.EndTime != DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime <= request.EndTime);
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (request.Interest != Commander.None)
        {
            if (request.Versus != Commander.None)
            {
                replays = replays.Where(x => x.Players.Any(a => a.Race == request.Interest && a.OppRace == request.Versus));
            }
            else
            {
                replays = replays.Where(x => x.Players.Any(a => a.Race == request.Interest));
            }
        }

        if (request.PlayerNames.Any())
        {
            replays = replays.Where(x => x.Players.Any(a => request.PlayerNames.Contains(a.Name)));
        }

        if (request.PlayerCount > 0)
        {
            replays = replays.Where(x => x.Playercount == request.PlayerCount);
        }

        return replays;
    }
}
