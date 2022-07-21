using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using sc2dsstats.db.Services;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static async Task<DsResponse> GetSynergy(sc2dsstatsContext context, DsRequest request)
        {
            var replays = ReplayFilter.Filter(context, request);

            var synergy = request.Player
                          ? from r in replays
                            from p in r.Dsplayers
                            from t in r.Dsplayers
                            where p.isPlayer && p.Race == (byte)DSData.GetCommander(request.Interest) && t.Team == p.Team && t.Id != p.Id
                            group new { r, p } by t.Race into g
                            select new DsResponseItem()
                            {
                                Label = ((DSData.Commander)g.Key).ToString(),
                                Count = g.Count(),
                                Wins = g.Count(c => c.p.Win),
                            }
                          : from r in replays
                            from p in r.Dsplayers
                            from t in r.Dsplayers
                            where p.Race == (byte)DSData.GetCommander(request.Interest) && t.Team == p.Team && t.Id != p.Id
                            group new { r, p } by t.Race into g
                            select new DsResponseItem()
                            {
                                Label = ((DSData.Commander)g.Key).ToString(),
                                Count = g.Count(),
                                Wins = g.Count(c => c.p.Win),
                            };
            var items = await synergy.ToListAsync();
            items = items.Where(x => DSData.cmdrs.Contains(x.Label)).OrderBy(o => o.Label).ToList();

            return new DsResponse()
            {
                Interest = request.Interest,
                Count = items.Sum(s => s.Count),
                Items = items
            };
        }

        public static async Task<DsResponse> GetAntiSynergy(sc2dsstatsContext context, DsRequest request)
        {
            var replays = ReplayFilter.Filter(context, request);

            var synergy = request.Player
                          ? from r in replays
                            from p in r.Dsplayers
                            from t in r.Dsplayers
                            where p.isPlayer && p.Race == (byte)DSData.GetCommander(request.Interest) && t.Team != p.Team
                            group new { r, p } by t.Race into g
                            select new DsResponseItem()
                            {
                                Label = ((DSData.Commander)g.Key).ToString(),
                                Count = g.Count(),
                                Wins = g.Count(c => c.p.Win),
                            }
                          : from r in replays
                            from p in r.Dsplayers
                            from t in r.Dsplayers
                            where p.Race == (byte)DSData.GetCommander(request.Interest) && t.Team != p.Team
                            group new { r, p } by t.Race into g
                            select new DsResponseItem()
                            {
                                Label = ((DSData.Commander)g.Key).ToString(),
                                Count = g.Count(),
                                Wins = g.Count(c => c.p.Win),
                            };
            var items = await synergy.ToListAsync();
            items = items.Where(x => DSData.cmdrs.Contains(x.Label)).OrderBy(o => o.Label).ToList();

            return new DsResponse()
            {
                Interest = request.Interest,
                Count = items.Sum(s => s.Count),
                Items = items
            };
        }
    }
}
