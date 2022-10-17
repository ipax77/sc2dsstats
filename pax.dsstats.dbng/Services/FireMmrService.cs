using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public class FireMmrService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;

    public FireMmrService(IServiceProvider serviceProvider, IMapper mapper)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
    }

    private static readonly double consistencyImpact = 0.50;
    private static readonly double consistencyDeltaMult = 0.15;

    private static readonly double eloK = 64; // default 32
    private static readonly double clip = eloK * 12.5;
    private static readonly double startMmr = 1000.0;

    private readonly Dictionary<int, List<DsRCheckpoint>> ratings = new();

    public async Task CalcMmmr()
    {
        await ClearRatings();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(r => r.Players)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Duration >= 300 && r.Maxleaver < 89 && r.WinnerTeam > 0)
            .Where(r => r.Playercount == 6 && r.GameMode == GameMode.Commanders)
            .OrderBy(r => r.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider);

        int count = 0;
        await replays.ForEachAsync(replay => {
            var winnerTeam = replay.Players.Where(x => x.Team == replay.WinnerTeam).Select(m => m.Player);
            var loserTeam = replay.Players.Where(x => x.Team != replay.WinnerTeam).Select(m => m.Player);

            if (count == 350)
            {
            }

            var winnerTeamMmr = GetTeamMmr(winnerTeam, replay.GameTime);
            var loserTeamMmr = GetTeamMmr(loserTeam, replay.GameTime);

            var teamElo = ExpectationToWin(winnerTeamMmr, loserTeamMmr);

            if (double.IsNaN(teamElo))
            {
            }

            CalculatePlayersDeltas(winnerTeam, true, teamElo, winnerTeamMmr,
                out double[] winnersMmrDelta, out double[] winnersConsistencyDelta);
            CalculatePlayersDeltas(loserTeam, false, teamElo, loserTeamMmr,
                out double[] losersMmrDelta, out double[] losersConsistencyDelta);

            FixMMR_Equality(winnersMmrDelta, losersMmrDelta);


            AddPlayersRankings(winnerTeam, winnersMmrDelta, winnersConsistencyDelta, replay.GameTime);
            AddPlayersRankings(loserTeam, losersMmrDelta, losersConsistencyDelta, replay.GameTime);

            count++;
        });

        await SetRatings();
    }

    private async Task SetRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int i = 0;
        foreach (var rating in ratings)
        {
            var player = await context.Players.FirstAsync(f => f.PlayerId == rating.Key);
            player.DsR = rating.Value.Last().DsR;
            player.DsROverTime = GetOverTimeRating(rating.Value);
            i++;
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

    private static void FixMMR_Equality(double[] team1_mmrDelta, double[] team2_mmrDelta)
    {
        double abs_sumTeam1_mmrDelta = Math.Abs(team1_mmrDelta.Sum());
        double abs_sumTeam2_mmrDelta = Math.Abs(team2_mmrDelta.Sum());

        for (int i = 0; i < 3; i++)
        {
            team1_mmrDelta[i] = team1_mmrDelta[i] *
                ((abs_sumTeam1_mmrDelta + abs_sumTeam2_mmrDelta) / (abs_sumTeam1_mmrDelta * 2));
            team2_mmrDelta[i] = team2_mmrDelta[i] *
                ((abs_sumTeam2_mmrDelta + abs_sumTeam1_mmrDelta) / (abs_sumTeam2_mmrDelta * 2));
        }
    }

    private static double GetCorrected_revConsistency(double raw_revConsistency)
    {
        return 1.0;

        //return ((1 - CONSISTENCY_IMPACT) + (Program.CONSISTENCY_IMPACT * raw_revConsistency));
    }

    private void CalculatePlayersDeltas(IEnumerable<PlayerDsRDto> teamPlayers, bool winner, double teamElo, double teamMmr,
        out double[] playersMmrDelta, out double[] playersConsistencyDelta)
    {
        playersMmrDelta = new double[teamPlayers.Count()];
        playersConsistencyDelta = new double[teamPlayers.Count()];

        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            var plRatings = ratings[teamPlayers.ElementAt(i).PlayerId];
            double playerMmr = plRatings.Last().DsR;
            double playerConsistency = plRatings.Last().Consistency;

            double factor_playerToTeamMates = PlayerToTeamMates(teamMmr, playerMmr);
            double factor_consistency = GetCorrected_revConsistency(1 - playerConsistency);

            double playerImpact = 1
                * factor_playerToTeamMates
                * factor_consistency;

            playersMmrDelta[i] = CalculateMmrDelta(teamElo, playerImpact);
            playersConsistencyDelta[i] = consistencyDeltaMult * 2 * (teamElo - 0.50);

            if (!winner)
            {
                playersMmrDelta[i] *= -1;
                playersConsistencyDelta[i] *= -1;
            }
        }
    }

    private void AddPlayersRankings(IEnumerable<PlayerDsRDto> teamPlayers, double[] playersMmrDelta, double[] playersConsistencyDelta, DateTime gameTime)
    {
        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            var plRatings = ratings[teamPlayers.ElementAt(i).PlayerId];

            double mmrBefore = plRatings.Last().DsR;
            double consistencyBefore = plRatings.Last().Consistency;

            double mmrAfter = mmrBefore + playersMmrDelta[i];
            double consistencyAfter = consistencyBefore + playersConsistencyDelta[i];

            plRatings.Add(new DsRCheckpoint() { DsR = mmrAfter, Consistency = consistencyAfter, Time = gameTime });
        }
    }

    private static double ExpectationToWin(double playerOneRating, double playerTwoRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (playerTwoRating - playerOneRating)));
    }

    private static double CalculateMmrDelta(double teamElo, double playerImpact)
    {
        return (double)(eloK * 1.0/*mcv*/ * (1 - teamElo) * playerImpact);
    }

    private static double PlayerToTeamMates(double winnerTeamMmr, double playerMmr)
    {
        if (winnerTeamMmr == 0)
        {
            return (1.0 / 3);
        }

        return playerMmr / winnerTeamMmr;
    }

    private double GetTeamMmr(IEnumerable<PlayerDsRDto> players, DateTime gameTime)
    {
        double teamMmr = 0;

        foreach (var player in players)
        {
            if (!ratings.ContainsKey(player.PlayerId))
            {
                ratings[player.PlayerId] = new List<DsRCheckpoint>() { new() { DsR = startMmr, Time = gameTime } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += ratings[player.PlayerId].Last().DsR;
            }
        }

        return teamMmr / 3.0;
    }
}