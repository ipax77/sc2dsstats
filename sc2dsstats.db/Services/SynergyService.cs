using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;

namespace sc2dsstats.db.Services
{
    public static class SynergyService
    {
        public static async Task<DsResponse> GetSynergy(sc2dsstatsContext context, DsRequest request)
        {

            var timestrings = DSData.Timestrings(request.Timespan);

            var results = context.DsTimeResults
                .Include(i => i.Teammates)
                .AsNoTracking()
                .Where(x => x.Player == request.Player && timestrings.Contains(x.Timespan) && x.Cmdr == request.Interest);

            var synresults = await results.ToListAsync();

            int tcount = synresults.Sum(s => s.Count);
            var response = new DsResponse()
            {
                Interest = request.Interest,
                Count = tcount,
                AvgDuration = (int)(synresults.Sum(s => s.Duration) / tcount),
                Items = new List<DsResponseItem>()
            };

            foreach (var cmdr in DSData.cmdrs)
            {
                var teammates = synresults.SelectMany(s => s.Teammates).Where(x => x.Cmdr == cmdr);
                int count = teammates.Sum(s => s.Count);
                response.Items.Add(new DsResponseItem()
                {
                    Label = cmdr,
                    Count = count / 6,
                    Wins = teammates.Sum(s => s.Wins)
                });
            }

            return response;
        }

        public static async Task<DsResponse> GetAntiSynergy(sc2dsstatsContext context, DsRequest request)
        {
            var timestrings = DSData.Timestrings(request.Timespan);

            var results = context.DsTimeResults
                .Include(i => i.Opponents)
                .AsNoTracking()
                .Where(x => x.Player == request.Player && timestrings.Contains(x.Timespan) && x.Cmdr == request.Interest);

            var synresults = await results.ToListAsync();

            int tcount = synresults.Sum(s => s.Count);
            var response = new DsResponse()
            {
                Interest = request.Interest,
                Count = tcount / 6,
                AvgDuration = (int)(synresults.Sum(s => s.Duration) / tcount),
                Items = new List<DsResponseItem>()
            };

            foreach (var cmdr in DSData.cmdrs)
            {
                var opponents = synresults.SelectMany(s => s.Opponents).Where(x => x.Cmdr == cmdr);
                int count = opponents.Sum(s => s.Count);
                response.Items.Add(new DsResponseItem()
                {
                    Label = cmdr,
                    Count = count / 6,
                    Wins = opponents.Sum(s => s.Wins)
                });
            }

            return response;
        }
    }
}
