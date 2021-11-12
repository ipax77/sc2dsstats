using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.db.Services
{
    public static class DpsService
    {
        public static async Task<DsResponse> GetDpsFromTimeResults(sc2dsstatsContext context, DsRequest request)
        {
            var timestrings = DSData.Timestrings(request.Timespan);

            var results = context.DsTimeResults.AsNoTracking().Where(x => x.Player == request.Player && timestrings.Contains(x.Timespan));

            if (request.Interest == "ALL")
                results = results.Where(x => x.Opp == String.Empty);
            else
                results = results.Where(x => x.Cmdr == request.Interest);

            var dpsresults = await results.ToListAsync();

            int count = dpsresults.Sum(s => s.Count);

            var response = new DsResponse()
            {
                Interest = request.Interest,
                Count = request.Player ? count : count / 6,
                AvgDuration = (int)(dpsresults.Sum(s => s.Duration) / count),
                Items = new List<DsResponseItem>()
            };

            foreach (var cmdr in DSData.cmdrs)
            {
                var cmdrresults = (request.Interest == "ALL") switch
                {
                    true => dpsresults.Where(x => x.Cmdr == cmdr).ToList(),
                    false => dpsresults.Where(x => x.Opp == cmdr).ToList(),
                };

                var army = cmdrresults.Sum(s => s.Army);
                var kills = cmdrresults.Sum(s => s.Kills);
                var dpv = army / kills;

                response.Items.Add(new DsResponseItem()
                {
                    Label = cmdr,
                    Count = (int)(dpv * 10000),
                    Wins = 10000
                });
            }

            return response;
        }

        public static async Task<DsResponse> GetDps(sc2dsstatsContext context, DsRequest request)
        {
            var data = await GetDpsData(context, request);

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

                decimal army = cmdrreps.Sum(s => s.Army);
                decimal kills = cmdrreps.Sum(s => s.Kills);
                var dpv = army / kills;

                response.Items.Add(new DsResponseItem()
                {
                    Label = cmdr.ToString(),
                    Count = (int)(dpv * 10000),
                    Wins = 10000
                });
            }

            return response;
        }

        private static async Task<List<DbStatsResult>> GetDpsData(sc2dsstatsContext context, DsRequest request)
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
                              Army = p.Army,
                              Kills = p.Killsum
                          };

            if (request.Interest != "ALL")
                results = results.Where(x => x.Race == (byte)DSData.GetCommander(request.Interest));

            if (request.Player)
                results = results.Where(x => x.Player);

            return await results.ToListAsync();
        }
    }
}
