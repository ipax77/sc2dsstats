using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{

    private async Task<StatsResponse> GetWinrate(StatsRequest request, List<CmdrStats> cmdrstats)
    {
        DateTime endTime = request.EndTime == null ? DateTime.Today.AddDays(1) : request.EndTime;

        var stats = cmdrstats.Where(x => x.Time >= request.StartTime && x.Time <= endTime);

        if (request.Interest != Commander.Protoss && request.Interest != Commander.Terran && request.Interest != Commander.Zerg)
        {
            var validCmdrs = Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => (int)x > 3).ToList();
            stats = stats.Where(x => validCmdrs.Contains(x.Race)
                && validCmdrs.Contains(x.OppRace)).ToList();
        }

        if (request.Interest != Commander.None)
        {
            stats = stats.Where(x => x.Race == request.Interest).ToList();
        }

        var data = request.Interest == Commander.None ?
            stats.GroupBy(g => g.Race).Select(s => new StatsResponseItem()
            {
                Label = s.Key.ToString(),
                Matchups = s.Sum(c => c.Count),
                Wins = s.Sum(c => c.Wins),
                duration = (long)s.Sum(c => c.Duration)
            }).ToList()
            :
            stats.GroupBy(g => g.OppRace).Select(s => new StatsResponseItem()
            {
                Label = s.Key.ToString(),
                Matchups = s.Sum(c => c.Count),
                Wins = s.Sum(c => c.Wins),
                duration = (long)s.Sum(c => c.Duration)
            }).ToList();

        (var countNotDefault, var countDefault) = await GetCount(request);

        return new StatsResponse()
        {
            Request = request,
            Items = data,
            CountDefaultFilter = countDefault,
            CountNotDefaultFilter = countNotDefault,
            AvgDuration = !data.Any() ? 0 : Convert.ToInt32(data.Select(s => s.duration / (double)s.Matchups).Average())
        };
    }
}
