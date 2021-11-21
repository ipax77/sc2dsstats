using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using static sc2dsstats._2022.Shared.DSData;

namespace sc2dsstats.db.Services
{
    public class ReplayFilter
    {
        public static HashSet<int> Gamemodes = new HashSet<int>()
                {
                    (int)Gamemode.Commanders,
                    (int)Gamemode.CommandersHeroic
                };

        public static IQueryable<Dsreplay> DefaultFilter(sc2dsstatsContext context)
        {
            return context.Dsreplays
                .Include(p => p.Dsplayers)
                .AsNoTracking()
                .Where(x =>
                    x.DefaultFilter == true
                );
        }

        public static IQueryable<Dsreplay> Filter(sc2dsstatsContext context, DsRequest request)
        {
            if (request.Filter == null || request.Filter.isDefault)
            {
                var dreplays = DefaultFilter(context);
                dreplays = dreplays.Where(x => x.Gametime >= request.StartTime);
                if (request.EndTime != DateTime.Today)
                    dreplays = dreplays.Where(x => x.Gametime <= request.EndTime);

                return dreplays;
            }

            return GetFilteredReplays(context, request.Filter, request.StartTime, request.EndTime);
        }

        public static IQueryable<Dsreplay> Filter(sc2dsstatsContext context, DsReplayRequest request)
        {
            if (request.Filter == null || request.Filter.isDefault)
            {
                return DefaultFilter(context);
            }
            else
            {
                return GetFilteredReplays(context, request.Filter, DateTime.MinValue, DateTime.Today);
            }
        }


        public static IQueryable<Dsreplay> GetFilteredReplays(sc2dsstatsContext context, DsFilter filter, DateTime start, DateTime end)
        {
            var replays = context.Dsreplays
            .Include(p => p.Dsplayers)
            .AsNoTracking()
            .Where(x =>
                x.Winner >= 0
                && x.Minarmy >= filter.MinArmy
                && x.Minincome >= filter.MinIncome
                && x.Minkillsum >= filter.MinKills
            );

            if (start != DateTime.MinValue)
                replays = replays.Where(x => x.Gametime >= start);

            if (end != DateTime.Today)
                replays = replays.Where(x => x.Gametime <= end);

            if (filter.PlayerCount > 0)
                replays = replays.Where(x => x.Playercount == filter.PlayerCount);

            if (filter.Mid)
                replays = replays.Where(x => x.Mid1 > 40 && x.Mid1 < 60);

            if (filter.MaxLeaver > 0)
                replays = replays.Where(x => x.Maxleaver < filter.MaxLeaver);

            if (filter.MinDuration > 0)
                replays = replays.Where(x => x.Duration >= filter.MinDuration);

            if (filter.MaxDuration > 0)
                replays = replays.Where(x => x.Duration <= filter.MaxDuration);

            if (filter.GameModes.Any())
                replays = replays.Where(x => filter.GameModes.Contains(x.Gamemode));

            if (filter.Players.Any())
            {
                replays = from r in replays
                          from p in r.Dsplayers
                          where filter.Players.Contains(p.Name)
                          select r;
                if (filter.Players.Count > 0)
                    return replays.Distinct();
            }
            return replays;
        }

        public static void SetDefaultFilter(List<Dsreplay> replays)
        {
            if (replays == null || !replays.Any())
                return;

            replays
                .Where(x => x.Duration > 5 * 60)
                .Where(x => x.Maxleaver < 90)
                .Where(x => x.Minarmy > 1500)
                .Where(x => x.Minincome > 1500)
                .Where(x => x.Minkillsum > 1500)
                .Where(x => x.Playercount == 6)
                .Where(x => x.Winner >= 0)
                .Where(x => Gamemodes.Contains(x.Gamemode))
                .ToList()
                .ForEach(f => f.DefaultFilter = true);
        }
    }
}
