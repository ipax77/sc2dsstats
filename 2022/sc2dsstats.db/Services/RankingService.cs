using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.db.Services
{
    public class RankingService
    {
        public static async Task<List<DsRankingResponse>> GetRanking(sc2dsstatsContext context)
        {
            var replays = ReplayFilter.DefaultFilter(context);
            replays = replays.Where(x => x.Gametime >= new DateTime(2018, 1, 1));
            var players = await GetDatasets(context);
            List<DsRankingResponse> rankings = new List<DsRankingResponse>();

            foreach (var ent in players)
            {
                var plReplays = from r in replays
                                from p in r.Dsplayers
                                where p.Name == ent.Key
                                select new RankingHelper()
                                {
                                    Id = r.Id,
                                    Gametime = r.Gametime,
                                    MVP = r.Maxkillsum == p.Killsum,
                                    WIN = p.Win,
                                    Commander = ((DSData.Commander)p.Race).ToString(),
                                    WithTeammates = r.Dsplayers.Where(x => x.Team == p.Team && x != p && x.isPlayer).Any()
                                };
                var plData = await plReplays.AsSplitQuery().ToListAsync();

                if (plData.Count < 20)
                    continue;

                int maincount = 0;
                string main = "";
                foreach (var cmdr in DSData.cmdrs)
                {
                    int c = plData.Where(x => x.Commander == cmdr).Count();
                    if (c > maincount)
                    {
                        maincount = c;
                        main = cmdr;
                    }
                }

                rankings.Add(new DsRankingResponse()
                {
                    Playername = !String.IsNullOrEmpty(ent.Value) ? ent.Value : ent.Key,
                    Games = plData.Count,
                    Wins = plData.Where(x => x.WIN).Count(),
                    MVPs = plData.Where(x => x.MVP).Count(),
                    MainCommander = main,
                    GamesMain = maincount,
                    Teamgames = plData.Where(x => x.WithTeammates).Count()
                });
            }
            return rankings;
        }

        public static async Task<List<KeyValuePair<string, string>>> GetDatasets(sc2dsstatsContext context)
        {
            return await context.DsPlayerNames.Where(x => x.LatestReplay > DateTime.Today.AddMonths(-3)).Select(s => new KeyValuePair<string, string>(s.DbId.ToString(), s.Name)).ToListAsync();
        }
    }

    public class RankingHelper
    {
        public int Id { get; set; }
        public DateTime Gametime { get; set; }
        public bool MVP { get; set; }
        public bool WIN { get; set; }
        public string Commander { get; set; }
        public bool WithTeammates { get; set; }
    }

}
