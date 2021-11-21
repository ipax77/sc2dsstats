using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using sc2dsstats.db.Services;
using System.Text.RegularExpressions;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static Regex d_rx = new Regex(@"^(\d)+");

        public static async Task<TimelineResponse> GetDuration(sc2dsstatsContext context, DsRequest request)
        {
            var replays = ReplayFilter.Filter(context, request);

            replays = replays.Where(x => x.Gametime >= request.StartTime);
            if (request.EndTime != DateTime.Today)
            {
                replays = replays.Where(x => x.Gametime <= request.EndTime);
            }

            var results = request.Player switch
            {
                true => from r in replays
                        from p in r.Dsplayers
                        where p.Race == (byte)DSData.GetCommander(request.Interest) && p.isPlayer
                        select new
                        {
                            r.Id,
                            r.Duration,
                            p.Win
                        },
                false => from r in replays
                         from p in r.Dsplayers
                         where p.Race == (byte)DSData.GetCommander(request.Interest)
                         select new
                         {
                             r.Id,
                             r.Duration,
                             p.Win
                         }
            };
            var lresults = await results.ToListAsync();

            TimelineResponse response = new TimelineResponse()
            {
                Interest = request.Interest,
                Count = lresults.Select(s => s.Id).Distinct().Count(),
                AvgDuration = lresults.Count == 0 ? 0 : (int)(lresults.Sum(s => s.Duration) / lresults.Count),
                Items = new List<DsResponseItem>()
            };

            if (lresults.Any())
            {
                for (int i = 0; i < DSData.durations.Length; i++)
                {
                    int startd = 0;
                    if (i > 0)
                    {
                        Match m = d_rx.Match(DSData.durations[i]);
                        if (m.Success)
                            startd = int.Parse(m.Value);
                    }
                    int endd = startd + 3;
                    if (i == 0)
                        endd = 8;
                    if (i == DSData.durations.Length - 1)
                        endd = 200;
                    var ilresults = lresults.Where(x => x.Duration > startd * 60 && x.Duration < endd * 60).ToList();


                    response.Items.Add(new DsResponseItem()
                    {
                        Label = $"{DSData.durations[i]} min ({ilresults.Count})",
                        Count = ilresults.Count,
                        Wins = ilresults.Where(x => x.Win == true).Count()
                    });
                }
            }
            var sma = new SimpleMovingAverage(4);
            response.SmaData = new List<double>();
            response.SmaData = TimelineService.GetNiceLineData(response.Items.Select(s => s.Count == 0 ? 0 : ((double)s.Wins * 100.0 / (double)s.Count)), 4);

            return response;
        }
    }
}
