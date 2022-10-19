﻿using Microsoft.EntityFrameworkCore;
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
    private async Task<StatsResponse> GetTimeline(StatsRequest request)
    {
        if (!request.DefaultFilter)
        {
            return await GetCustomTimeline(request);
        }

        DateTime endTime = request.EndTime == DateTime.MinValue ? DateTime.Today.AddDays(1) : request.EndTime;

        var cmdrstats = await GetRequestStats(request);

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
                response.Items.Add(new()
                {
                    Label = $"{requestTime.ToString("yyyy-MM")} (0)",
                    Matchups = 0,
                    Wins = 0
                });
            }
            else
            {
                int ccount = timeResults.Sum(s => s.Count);
                response.Items.Add(new()
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

    public async Task<StatsResponse> GetCustomTimeline(StatsRequest request)
    {
        var lresults = await GetTimelineData(request);

        (var countNotDefault, var countDefault) = await GetCount(request);

        StatsResponse response = new StatsResponse()
        {
            Request = request,
            CountDefaultFilter = countDefault,
            CountNotDefaultFilter = countNotDefault,
            AvgDuration = lresults.Count == 0 ? 0 : (int)(lresults.Sum(s => s.Duration) / (double)lresults.Count),
            Items = new List<StatsResponseItem>()
        };

        DateTime _dateTime = request.StartTime;

        while (_dateTime < request.EndTime)
        {
            DateTime dateTime = _dateTime.AddMonths(1);
            var stepResults = lresults.Where(x => x.GameTime >= _dateTime && x.GameTime < dateTime).ToList();

            response.Items.Add(new()
            {
                Label = $"{_dateTime.ToString("yyyy-MM-dd")} ({stepResults.Count})",
                Matchups = stepResults.Count,
                Wins = stepResults.Where(x => x.Win == true).Count()
            });
            _dateTime = dateTime;
        }
        return response;
    }

    private async Task<List<DbStatsResult>> GetTimelineData(StatsRequest request)
    {
        var replays = GetCountReplays(request);

        var results =   from r in replays
                        from p in r.Players
                        where p.Race == request.Interest
                        select new DbStatsResult()
                        {
                            Id = r.ReplayId,
                            Win = p.PlayerResult == PlayerResult.Win,
                            GameTime = r.GameTime,
                            IsUploader = p.IsUploader,
                            OppRace = p.OppRace
                        };
        if (request.Uploaders)
        {
            results = results.Where(x => x.IsUploader);
        }

        if (request.Versus != Commander.None)
        {
            results = results.Where(x => x.OppRace == request.Versus);
        }

        return await results.ToListAsync();
    }
}

public record DbStatsResult
{
    public int DbStatsResultId { get; init; }
    public int Id { get; init; }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int Duration { get; init; }
    public bool Win { get; init; }
    public bool IsUploader { get; init; }
    public int Army { get; init; }
    public int Kills { get; init; }
    public bool MVP { get; init; }
    public DateTime GameTime { get; init; }
}