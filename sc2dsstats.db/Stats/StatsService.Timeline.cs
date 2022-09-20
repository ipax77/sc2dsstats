using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using sc2dsstats.shared;
using sc2dsstats.db.Services;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static TimelineResponse GetTimeline(TimelineRequest request, List<CmdrStats> cmdrstats)
        {
            if (request.Step < 3)
            {
                request.Step = 3;
            }

            if (request.smaK < 3)
            {
                request.smaK = 3;
            }

            var stats = GetTimeStats(request, cmdrstats);

            int tcount = stats.Sum(s => s.count);
            TimelineResponse response = new TimelineResponse()
            {
                Interest = request.Interest,
                Count = request.Player ? tcount : tcount / 6,
                Versus = request.Versus,
                AvgDuration = tcount == 0 ? 0 : (int)(stats.Sum(s => s.duration) / tcount),
                Items = new List<DsResponseItem>()
            };

            DateTime requestTime = request.StartTime;
            while (requestTime < request.EndTime)
            {
                var timeResults = stats.Where(f => f.year == requestTime.Year && f.month == requestTime.Month && f.RACE == (byte)DSData.GetCommander(request.Interest));

                if (!timeResults.Any())
                {
                    response.Items.Add(new TimelineResponseItem()
                    {
                        Label = $"{requestTime.ToString("yyyy-MM")} (0)",
                        Count = 0,
                        Wins = 0
                    });
                }
                else
                {
                    int ccount = timeResults.Sum(s => s.count);
                    response.Items.Add(new TimelineResponseItem()
                    {
                        Label = $"{requestTime.ToString("yyyy-MM")} ({ccount})",
                        Count = ccount,
                        Wins = timeResults.Sum(s => s.wins)
                    });
                }
                requestTime = requestTime.AddMonths(1);
            }

            if (response.Items.Any() && response.Items.Last().Count < 10)
                response.Items.RemoveAt(response.Items.Count - 1);

            var sma = new SimpleMovingAverage(request.smaK);
            response.SmaData = new List<double>();
            response.SmaData = GetNiceLineData(response.Items.Select(s => s.Count == 0 ? 0 : ((double)s.Wins * 100.0 / (double)s.Count)), request.smaK);

            return response;
        }

        public static async Task<TimelineResponse> GetCustomTimeline(sc2dsstatsContext context, TimelineRequest request)
        {
            if (request.Step < 3)
            {
                request.Step = 3;
            }

            if (request.smaK < 3)
            {
                request.smaK = 3;
            }

            var lresults = await GetTimelineData(request, context);

            TimelineResponse response = new TimelineResponse()
            {
                Count = lresults.Select(s => s.Id).Distinct().Count(),
                Interest = request.Interest,
                Versus = request.Versus,
                AvgDuration = lresults.Count == 0 ? 0 : (int)(lresults.Sum(s => s.Duration) / (double)lresults.Count),
                Items = new List<DsResponseItem>()
            };

            DateTime _dateTime = request.StartTime;

            while (_dateTime < request.EndTime)
            {
                DateTime dateTime = _dateTime.AddMonths(1);
                var stepResults = lresults.Where(x => x.GameTime >= _dateTime && x.GameTime < dateTime).ToList();

                response.Items.Add(new TimelineResponseItem()
                {
                    Label = $"{_dateTime.ToString("yyyy-MM-dd")} ({stepResults.Count})",
                    Count = stepResults.Count,
                    Wins = stepResults.Where(x => x.Win == true).Count()
                });
                _dateTime = dateTime;
            }

            if (response.Items.Any() && response.Items.Last().Count < 10)
                response.Items.RemoveAt(response.Items.Count - 1);

            var sma = new SimpleMovingAverage(request.smaK);
            response.SmaData = new List<double>();

            response.SmaData = GetNiceLineData(response.Items.Select(s => s.Count == 0 ? 0 : ((double)s.Wins * 100.0 / (double)s.Count)), request.smaK);

            return response;
        }

        public static async Task<List<DbStatsResult>> GetTimelineData(TimelineRequest request, sc2dsstatsContext context)
        {
            var replays = ReplayFilter.Filter(context, request);

            var results = from r in replays
                          from p in r.Dsplayers
                          where p.Race == (byte)DSData.GetCommander(request.Interest)
                          select new DbStatsResult()
                          {
                              Id = r.Id,
                              Win = p.Win,
                              GameTime = r.Gametime,
                              Player = p.isPlayer,
                              OppRace = p.Opprace
                          };

            if (request.Player)
                results = results.Where(x => x.Player);

            if (request.Versus != "ALL")
                results = results.Where(x => x.OppRace == (byte)DSData.GetCommander(request.Versus));

            return await results.ToListAsync();
        }

        public static List<double> GetNiceLineData(IEnumerable<double> data, int order)
        {
            List<double> xdata = new List<double>();
            for (int i = 0; i < data.Count(); i++)
            {
                xdata.Add(i);
            }

            if (xdata.Count < 4)
                return new List<double>();

            if (xdata.Count() < order)
                order = Math.Max(xdata.Count() - 2, 3);

            var poly = Fit.PolynomialFunc(xdata.ToArray(), data.ToArray(), order);

            List<double> nicedata = new List<double>();
            for (int i = 0; i < data.Count(); i++)
            {
                nicedata.Add(Math.Round(poly(i), 2));
            }

            return nicedata;
        }
    }
}
