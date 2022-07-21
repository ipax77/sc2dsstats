using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;

namespace sc2dsstats.db.Services
{
    public class WinrateService
    {
        public static async Task<DsResponse> GetWinrateFromTimeResults(sc2dsstatsContext context, DsRequest request)
        {
            var timestrings = DSData.Timestrings(request.Timespan);

            var results = context.DsTimeResults.AsNoTracking().Where(x => x.Player == request.Player && timestrings.Contains(x.Timespan));

            if (request.Interest == "ALL")
                results = results.Where(x => x.Opp == String.Empty);
            else
                results = results.Where(x => x.Cmdr == request.Interest);

            var timeresults = await results.ToListAsync();

            int tcount = timeresults.Sum(s => s.Count);
            var response = new DsResponse()
            {
                Interest = request.Interest,
                Count = timeresults.Sum(s => s.Count) / 6,
                AvgDuration = tcount == 0 ? 0 : (int)(timeresults.Sum(s => s.Duration) / tcount),
                Items = new List<DsResponseItem>()
            };

            foreach (var cmdr in DSData.cmdrs)
            {
                var cmdrresults = (request.Interest == "ALL") switch
                {
                    true => timeresults.Where(x => x.Cmdr == cmdr).ToList(),
                    false => timeresults.Where(x => x.Opp == cmdr).ToList(),
                };
                int count = cmdrresults.Sum(s => s.Count);
                response.Items.Add(new DsResponseItem()
                {
                    Label = cmdr,
                    Count = count,
                    Wins = cmdrresults.Sum(s => s.Wins)
                });
            }

            return response;
        }

        public static async Task<DsResponse> GetWinrate(sc2dsstatsContext context, DsRequest request)
        {
            var data = await GetWinrateData(context, request);

            var response = new DsResponse()
            {
                Interest = request.Interest,
                Count = data.Select(s => s.Id).Distinct().Count(),
                Items = new List<DsResponseItem>()
            };
            response.AvgDuration = (int)((double)data.Sum(s => s.Duration) / (double)data.Count);

            foreach (var cmdr in DSData.GetCommanders)
            {
                var cmdrreps = (request.Interest == "ALL") switch
                {
                    true => data.Where(x => x.Race == (byte)cmdr),
                    false => data.Where(x => x.OppRace == (byte)cmdr)
                };

                response.Items.Add(new DsResponseItem()
                {
                    Label = cmdr.ToString(),
                    Count = cmdrreps.Count(),
                    Wins = cmdrreps.Where(x => x.Win == true).Count()
                });
            }

            return response;
        }

        private static async Task<List<DbStatsResult>> GetWinrateData(sc2dsstatsContext context, DsRequest request)
        {
            var replays = ReplayFilter.Filter(context, request);

            var results = from r in replays
                          from p in r.Dsplayers
                          select new DbStatsResult()
                          {
                              Id = r.Id,
                              Race = p.Race,
                              OppRace = p.Opprace,
                              Duration = r.Duration,
                              Win = p.Win,
                              Player = p.isPlayer
                          };

            if (request.Interest != "ALL")
                results = results.Where(x => x.Race == (byte)DSData.GetCommander(request.Interest));

            if (request.Player)
                results = results.Where(x => x.Player);

            return await results.ToListAsync();
        }
    }
}
