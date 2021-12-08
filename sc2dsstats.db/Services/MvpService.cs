using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;

namespace sc2dsstats.db.Services
{
    public static class MvpService
    {
        public static async Task<DsResponse> GetMvpFromTimeResults(sc2dsstatsContext context, DsRequest request)
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
                Count = request.Player ? tcount : tcount / 6,
                AvgDuration = (int)(timeresults.Sum(s => s.Duration) / tcount),
                Items = new List<DsResponseItem>()
            };

            foreach (var cmdr in DSData.cmdrs)
            {
                var cmdrresults = (request.Interest == "ALL") switch
                {
                    true => timeresults.Where(x => x.Cmdr == cmdr).ToList(),
                    false => timeresults.Where(x => x.Opp == cmdr).ToList(),
                };
                response.Items.Add(new DsResponseItem()
                {
                    Label = cmdr,
                    Count = cmdrresults.Sum(s => s.Count),
                    Wins = cmdrresults.Sum(s => s.MVP)
                });
            }

            return response;
        }

        public static async Task<DsResponse> GetMvp(sc2dsstatsContext context, DsRequest request)
        {
            var data = await GetMVPData(context, request);

            var response = new DsResponse()
            {
                Interest = request.Interest,
                Count = data.Select(s => s.Id).Distinct().Count(),
                Items = new List<DsResponseItem>()
            };
            response.AvgDuration = (int)((double)data.Sum(s => s.Duration) / (double)data.Count);

            foreach (var cmdr in Enum.GetValues<DSData.Commander>())
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
                    Wins = cmdrreps.Where(x => x.MVP == true).Count()
                });
            }

            return response;
        }

        private static async Task<List<DbStatsResult>> GetMVPData(sc2dsstatsContext context, DsRequest request)
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
                              Player = p.isPlayer,
                              MVP = p.Killsum == r.Maxkillsum
                          };

            if (request.Interest != "ALL")
                results = results.Where(x => x.Race == (byte)DSData.GetCommander(request.Interest));

            if (request.Player)
                results = results.Where(x => x.Player);

            return await results.ToListAsync();
        }
    }
}
