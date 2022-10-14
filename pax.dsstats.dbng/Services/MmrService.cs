using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public class MmrService
{
    private readonly ReplayContext context;

    public MmrService(ReplayContext context)
    {
        this.context = context;
    }

    private Dictionary<int, double> toonIdRatings = new();


    public async Task CalcMmmr()
    {
        toonIdRatings.Clear();

        var replays = context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .Where(x => x.Duration >= 300 && x.Maxleaver < 89 && x.WinnerTeam > 0)
            .Where(x => x.GameMode == shared.GameMode.Commanders)
            .OrderBy(o => o.GameTime)
            .AsNoTracking();


        await replays.ForEachAsync(f =>
        {
            var winnerTeamIds = f.Players.Where(x => x.Team == f.WinnerTeam).Select(m => m.Player.ToonId);
            var runnerTeamIds = f.Players.Where(x => x.Team != f.WinnerTeam).Select(m => m.Player.ToonId);

            var winnerTeamMmr = GetTeamMmr(winnerTeamIds);
            var runnerTeamMmr = GetTeamMmr(runnerTeamIds);

            var delta = CalculateELODelta(winnerTeamMmr, runnerTeamMmr);

            SetWinnerMmr(winnerTeamIds, delta);
            SetRunnerMmr(runnerTeamIds, delta);
        });

        foreach (var ent in toonIdRatings.OrderByDescending(o => o.Value).Take(15))
        {
            var dbPl = await context.Players.FirstAsync(f => f.ToonId == ent.Key);
            Console.WriteLine($"{dbPl.Name}|{ent.Key} => {ent.Value:N2}");
        }
    }

    private void SetRunnerMmr(IEnumerable<int> runnerTeamIds, double delta)
    {
        foreach (var id in runnerTeamIds)
        {
            toonIdRatings[id] -= delta;
        }
    }

    private void SetWinnerMmr(IEnumerable<int> winnerTeamIds, double delta)
    {
        foreach (var id in winnerTeamIds)
        {
            toonIdRatings[id] += delta;
        }
    }

    private static double ExpectationToWin(double playerOneRating, double playerTwoRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (playerTwoRating - playerOneRating) / 400.0));
    }

    private static double CalculateELODelta(double winnerTeamMmr, double runnerTeamMmr)
    {
        int eloK = 32;

        return (double)(eloK * (1.0 - ExpectationToWin(winnerTeamMmr, runnerTeamMmr)));
    }

    private double GetTeamMmr(IEnumerable<int> playerToonIds)
    {
        double teamMmr = 0;

        foreach (var playerToonId in playerToonIds) {
            if (!toonIdRatings.ContainsKey(playerToonId))
            {
                toonIdRatings[playerToonId] = 1000;
                teamMmr += 1000;
            }
            else
            {
                teamMmr += toonIdRatings[playerToonId];
            }
        }
        return teamMmr;
    }

}
