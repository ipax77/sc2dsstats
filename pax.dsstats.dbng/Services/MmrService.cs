using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public class MmrService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;

    public MmrService(IServiceProvider serviceProvider, IMapper mapper)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
    }
    private readonly int eloK = 35; // default 35
    private readonly double startMmr = 1000.0;
    private readonly Dictionary<int, List<DsRCheckpoint>> ratings = new();

    public event EventHandler<EventArgs>? Recalculated;
    protected virtual void OnRecalculated(EventArgs e)
    {
        EventHandler<EventArgs>? handler = Recalculated;
        handler?.Invoke(this, e);
    }

    public async Task CalcMmmr()
    {
        await ClearRatings();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        
        var replays = context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .Where(x => x.Duration >= 300 && x.Maxleaver < 89 && x.WinnerTeam > 0)
            .Where(x => x.Playercount == 6 && x.GameMode == shared.GameMode.Commanders)
            // .Where(x => x.Playercount == 2)
            .OrderBy(o => o.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider);


        await replays.ForEachAsync(f =>
        {
            var winnerTeam = f.Players.Where(x => x.Team == f.WinnerTeam).Select(m => m.Player);
            var runnerTeam = f.Players.Where(x => x.Team != f.WinnerTeam).Select(m => m.Player);

            var winnerTeamMmr = GetTeamMmr(winnerTeam, f.GameTime);
            var runnerTeamMmr = GetTeamMmr(runnerTeam, f.GameTime);

            var delta = CalculateELODelta(winnerTeamMmr, runnerTeamMmr);

            SetWinnerMmr(winnerTeam, delta, f.GameTime);
            SetRunnerMmr(runnerTeam, delta, f.GameTime);
        });

        //foreach (var rating in ratings.OrderByDescending(o => o.Value.Last().DsR).Take(15))
        //{
        //    Console.WriteLine($"{rating.Key} => {rating.Value.Last().DsR:N2}");
        //}

        await SetRatings();
        OnRecalculated(new());
    }

    private async Task SetRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int i = 0;
        foreach (var ent in ratings)
        {
            var player = await context.Players.FirstAsync(f => f.PlayerId == ent.Key);
            player.Mmr = ent.Value.Last().DsR;
            player.MmrOverTime = GetOverTimeRating(ent.Value);
            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();
    }

    private static string? GetOverTimeRating(List<DsRCheckpoint> dsRCheckpoints)
    {
        if (dsRCheckpoints.Count == 0)
        {
            return null;
        }

        else if (dsRCheckpoints.Count == 1)
        {
            return $"{Math.Round(dsRCheckpoints[0].DsR, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[0].Time:MMyy}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(dsRCheckpoints.First().DsR, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.First().Time:MMyy}");

        if (dsRCheckpoints.Count > 2)
        {
            string timeStr = dsRCheckpoints[0].Time.ToString(@"MMyy");
            for (int i = 1; i < dsRCheckpoints.Count - 1; i++)
            {
                string currentTimeStr = dsRCheckpoints[i].Time.ToString(@"MMyy");
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(dsRCheckpoints[i].DsR, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[i].Time:MMyy}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(dsRCheckpoints.Last().DsR, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.Last().Time:MMyy}");

        if (sb.Length > 1999)
        {
            throw new ArgumentOutOfRangeException(nameof(dsRCheckpoints));
        }

        return sb.ToString();
    }

    private async Task ClearRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // todo: db-lock (no imports possible during this)
        await context.Database.ExecuteSqlRawAsync($"UPDATE Players SET DsR = {startMmr}");
        await context.Database.ExecuteSqlRawAsync("UPDATE Players SET DsROverTime = NULL");
        ratings.Clear();
    }

    private void SetRunnerMmr(IEnumerable<PlayerDsRDto> teamPlayers, double delta, DateTime gameTime)
    {
        foreach (var player in teamPlayers)
        {
            var plRatings = ratings[player.PlayerId];
            var newRating = plRatings.Last().DsR - delta;
            plRatings.Add(new DsRCheckpoint() { DsR = newRating, Time = gameTime });
        }
    }

    private void SetWinnerMmr(IEnumerable<PlayerDsRDto> teamPlayers, double delta, DateTime gameTime)
    {
        foreach (var player in teamPlayers)
        {
            var plRatings = ratings[player.PlayerId];
            var newRating = plRatings.Last().DsR + delta;
            plRatings.Add(new DsRCheckpoint() { DsR = newRating, Time = gameTime });
        }
    }

    private static double ExpectationToWin(double playerOneRating, double playerTwoRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (playerTwoRating - playerOneRating) / 400.0));
    }

    private double CalculateELODelta(double winnerTeamMmr, double runnerTeamMmr)
    {

        return (double)(eloK * (1.0 - ExpectationToWin(winnerTeamMmr, runnerTeamMmr)));
    }

    private double GetTeamMmr(IEnumerable<PlayerDsRDto> players, DateTime gameTime)
    {
        double teamMmr = 0;

        foreach (var player in players)
        {
            if (!ratings.ContainsKey(player.PlayerId))
            {
                ratings[player.PlayerId] = new List<DsRCheckpoint>() { new() { DsR = 1000.0, Time = gameTime } };
                teamMmr += 1000.0;
            }
            else
            {
                teamMmr += ratings[player.PlayerId].Last().DsR;
            }
        }
        return teamMmr / 3.0;
    }
}

public record DsRCheckpoint
{
    public double Consistency { get; init; }
    public double DsR { get; init; }
    public DateTime Time { get; init; }
}
