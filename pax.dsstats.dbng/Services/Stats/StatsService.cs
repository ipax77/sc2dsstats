using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public partial class StatsService : IStatsService
{
    private readonly IMemoryCache memoryCache;
    private readonly ReplayContext context;
    private readonly IMapper mapper;

    public StatsService(IMemoryCache memoryCache, ReplayContext context, IMapper mapper)
    {
        this.memoryCache = memoryCache;
        this.context = context;
        this.mapper = mapper;
    }

    public async Task<StatsResponse> GetStatsResponse(StatsRequest request)
    {
        string memKey = request.Uploaders ? "cmdrstatsuploaders" : "cmdrstats";
        if (!memoryCache.TryGetValue(memKey, out List<CmdrStats> stats))
        {
            if (request.Uploaders)
            {
                stats = await GetUploaderStats();
            }
            else
            {
                stats = await GetStats();
            }
            memoryCache.Set(memKey, stats, new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.High)
                .SetAbsoluteExpiration(TimeSpan.FromDays(1))
            );
        }

        return request.StatsMode switch
        {
            StatsMode.Winrate => await GetWinrate(request, stats),
            StatsMode.Timeline => await GetTimeline(request, stats),
            _ => new()
        };
    }

    public void ResetCache()
    {
        memoryCache.Remove("cmdrstatsuploaders");
        memoryCache.Remove("cmdrstats");
    }

    private async Task<List<CmdrStats>> GetUploaderStats()
    {
        var stats = from r in context.Replays
                    from p in r.Players
                    where p.IsUploader
                    group new { r, p } by new { year = r.GameTime.Year, month = r.GameTime.Month, race = p.Race, opprace = p.OppRace } into g
                    select new CmdrStats()
                    {
                        Year = g.Key.year,
                        Month = g.Key.month,
                        Race = g.Key.race,
                        OppRace = g.Key.opprace,
                        Count = g.Count(),
                        Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                        Mvp = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                        Army = g.Sum(s => s.p.Army),
                        Kills = g.Sum(s => s.p.Kills),
                        Duration = g.Sum(s => s.r.Duration),
                    };
        return await stats.ToListAsync();
    }

    private async Task<List<CmdrStats>> GetStats()
    {
        var stats = from r in context.Replays
                    from p in r.Players
                    where r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic
                        // where r.DefaultFilter && p.IsUploader
                    group new { r, p } by new { year = r.GameTime.Year, month = r.GameTime.Month, race = p.Race, opprace = p.OppRace } into g
                    select new CmdrStats()
                    {
                        Year = g.Key.year,
                        Month = g.Key.month,
                        Time = new DateTime(g.Key.year, g.Key.month, 1),
                        Race = g.Key.race,
                        OppRace = g.Key.opprace,
                        Count = g.Count(),
                        Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                        Mvp = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                        Army = g.Sum(s => s.p.Army),
                        Kills = g.Sum(s => s.p.Kills),
                        Duration = g.Sum(s => s.r.Duration),
                    };
        return await stats.ToListAsync();
    }


    private async Task<StatsResponse> GetTimeline(StatsRequest request, List<CmdrStats> cmdrstats)
    {
        DateTime endTime = request.EndTime == null ? DateTime.Today.AddDays(1) : request.EndTime;

        var stats = cmdrstats.Where(x => x.Time >= request.StartTime && x.Time <= endTime);

        int tcount = stats.Sum(s => s.Count);

        (var countNotDefault, var countDefault) = await GetCount(request);

        StatsResponse response = new()
        {
            Request = request,
            CountDefaultFilter = countDefault,
            CountNotDefaultFilter = countNotDefault,
            AvgDuration = tcount == 0 ? 0 : (int)stats.Sum(s => s.Duration) / tcount,
            Items = new List<StatsResponseItem>()
        };

        DateTime requestTime = request.StartTime;

        while (requestTime < endTime)
        {
            var timeResults = stats.Where(f => f.Year == requestTime.Year && f.Month == requestTime.Month && f.Race == request.Interest);

            if (!timeResults.Any())
            {
                response.Items.Add(new ()
                {
                    Label = $"{requestTime.ToString("yyyy-MM")} (0)",
                    Matchups = 0,
                    Wins = 0
                });
            }
            else
            {
                int ccount = timeResults.Sum(s => s.Count);
                response.Items.Add(new ()
                {
                    Label = $"{requestTime.ToString("yyyy-MM")} ({ccount})",
                    Matchups = ccount,
                    Wins = timeResults.Sum(s => s.Wins)
                });
            }
            requestTime = requestTime.AddMonths(1);
        }

        var lastItem = response.Items.LastOrDefault();
        if (lastItem != null && lastItem.Matchups < 10)
        {
            response.Items.Remove(lastItem);
        }

        return response;
    }
}

public record CmdrStats
{
    public int Year { get; init; }
    public int Month { get; init; }
    public DateTime Time { get; init; }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public decimal Army { get; init; }
    public decimal Kills { get; init; }
    public decimal Duration { get; init; }
    public int Replays { get; init; }
}