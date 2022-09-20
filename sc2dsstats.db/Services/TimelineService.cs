using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using sc2dsstats.shared;

namespace sc2dsstats.db.Services
{
    public static class TimelineService
    {
        public static async Task<TimelineResponse> GetTimelineFromTimeResults(sc2dsstatsContext context, TimelineRequest request)
        {
            if (request.Step < 3)
            {
                request.Step = 3;
            }

            if (request.smaK < 3)
            {
                request.smaK = 3;
            }



            DateTime requestTime = request.StartTime;

            List<string> timeRequestStrings = new List<string>();
            while (requestTime < request.EndTime)
            {
                timeRequestStrings.Add(requestTime.ToString("yyyyMM"));
                requestTime = requestTime.AddMonths(1);
            }

            List<DsTimeResult> timeResults = await context.DsTimeResults
                .AsNoTracking()
                .Where(f =>
                    f.Cmdr == request.Interest &&
                    f.Opp == String.Empty
                    && timeRequestStrings.Contains(f.Timespan)
                    && f.Player == request.Player
                )
                .ToListAsync();

            int tcount = timeResults.Sum(s => s.Count);
            TimelineResponse response = new TimelineResponse()
            {
                Interest = request.Interest,
                Count = tcount / 6,
                Versus = request.Versus,
                AvgDuration = (int)(timeResults.Sum(s => s.Duration) / tcount),
                Items = new List<DsResponseItem>()
            };

            requestTime = request.StartTime;
            while (requestTime < request.EndTime)
            {
                var timeResult = timeResults.SingleOrDefault(f => f.Timespan == requestTime.ToString("yyyyMM"));

                if (timeResult == null || timeResult.Count == 0)
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
                    response.Items.Add(new TimelineResponseItem()
                    {
                        Label = $"{requestTime.ToString("yyyy-MM")} ({timeResult.Count})",
                        Count = timeResult.Count,
                        Wins = timeResult.Wins
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

        public static async Task<TimelineResponse> GetTimeline(sc2dsstatsContext context, TimelineRequest request)
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
                AvgDuration = (int)(lresults.Sum(s => s.Duration) / (double)lresults.Count),
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
