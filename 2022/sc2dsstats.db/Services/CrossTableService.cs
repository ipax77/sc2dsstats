using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.db.Services
{
    public class CrossTableService
    {
        public static async Task<DsResponse> GetCrosstableFromTimeResults(sc2dsstatsContext context, DsRequest request)
        {
            var timestrings = DSData.Timestrings(request.Timespan, true);

            var results = await context.DsTimeResults
                .AsNoTracking()
                .Where(x =>
                    x.Player == request.Player &&
                    timestrings.Contains(x.Timespan))
                .ToListAsync();

            CrosstableResponse response = new CrosstableResponse()
            {
                Interest = "CrossTable",
                Items = new List<CrosstableResponseItem>()
            };

            var removeMonths = new List<string>() { DateTime.Today.ToString("yyyyMM") };
            if (DateTime.Today.Day < 16)
            {
                removeMonths.Add((DateTime.Today.AddMonths(-1)).ToString("yyyyMM"));
            }

            foreach (var cmdr in DSData.cmdrs)
            {
                var cmdrResults = results.Where(x => x.Cmdr == cmdr).ToArray();

                foreach (var vs in DSData.cmdrs)
                {
                    if (vs == cmdr)
                        response.Items.Add(new CrosstableResponseItem());
                    else
                    {
                        var vsResults = cmdrResults.Where(x => x.Opp == vs).ToArray();
                        var oldVsResults = vsResults.Where(x => !removeMonths.Contains(x.Timespan)).ToArray();
                        response.Items.Add(new CrosstableResponseItem()
                        {
                            Label = $"{cmdr} vs {vs}",
                            Count = vsResults.Sum(s => s.Count),
                            Wins = vsResults.Sum(s => s.Wins),
                            OldCount = oldVsResults.Sum(s => s.Count),
                            OldWins = oldVsResults.Sum(s => s.Wins),
                        });
                    }
                }
            }
            return response;


        }

        public static async Task<DsResponse> GetCrossTableData(DsRequest request, sc2dsstatsContext context)
        {
            var results = await GetData(request, context);


            CrosstableResponse response = new CrosstableResponse()
            {
                Interest = "CrossTable",
                Items = new List<CrosstableResponseItem>()
            };

            foreach (var cmdr in Enum.GetValues<DSData.Commander>())
            {
                var cmdrResults = results.Where(x => x.Race == (byte)cmdr).ToArray();

                foreach (var vs in Enum.GetValues<DSData.Commander>())
                {
                    if (vs == cmdr)
                        response.Items.Add(new CrosstableResponseItem());
                    else
                    {
                        var vsResults = cmdrResults.Where(x => x.OppRace == (byte)vs).ToArray();
                        var oldVsResults = vsResults.Where(x => x.GameTime < request.EndTime.AddDays(DateTime.Today.Day < 16  ? -60 : -30)).ToArray();
                        response.Items.Add(new CrosstableResponseItem()
                        {
                            Label = $"{cmdr} vs {vs}",
                            Count = vsResults.Length,
                            Wins = vsResults.Where(x => x.Win == true).Count(),
                            OldCount = oldVsResults.Length,
                            OldWins = oldVsResults.Where(x => x.Win == true).Count(),
                        });
                    }
                }
            }
            return response;
        }

        private static async Task<List<DbStatsResult>> GetData(DsRequest request, sc2dsstatsContext context)
        {
            var replays = ReplayFilter.DefaultFilter(context);

            var reps = request.Player switch
            {
                false => from r in replays
                         from p in r.Dsplayers
                         where r.Gametime >= request.StartTime && r.Gametime <= request.EndTime
                         select new DbStatsResult()
                         {
                             Id = r.Id,
                             Win = p.Win,
                             Race = p.Race,
                             OppRace = p.Opprace,
                             GameTime = r.Gametime
                         },
                true => from r in replays
                        from p in r.Dsplayers
                        where r.Gametime >= request.StartTime && r.Gametime <= request.EndTime
                            && p.isPlayer
                        select new DbStatsResult()
                        {
                            Id = r.Id,
                            Win = p.Win,
                            Race = p.Race,
                            OppRace = p.Opprace,
                            GameTime = r.Gametime
                        }
            };

            return await reps.ToListAsync();

            //var dbresults = context.DbStatsResults
            // .AsNoTracking()
            // .Where(x =>
            //       x.GameTime >= request.StartTime
            //    && x.GameTime <= request.EndTime
            //);

            //if (request.Player)
            //{
            //    dbresults = dbresults.Where(x => x.Player == true);
            //}

            // return await dbresults.ToListAsync();
        }

        private static int OppPos(int pos)
        {
            return pos switch
            {
                1 => 4,
                2 => 5,
                3 => 6,
                4 => 1,
                5 => 2,
                6 => 3,
                _ => pos
            };
        }
    }
}
