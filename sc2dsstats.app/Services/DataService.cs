using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using sc2dsstats.db.Stats;
using System.Collections.Concurrent;

namespace sc2dsstats.app.Services
{
    public class DataService : IDataService
    {
        private readonly sc2dsstatsContext context;
        private readonly IMemoryCache memoryCache;
        private readonly IHttpClientFactory clientFactory;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<DataService> logger;
        private static ConcurrentDictionary<string, PlayerNameStatsResponse> PlayerStats = new ConcurrentDictionary<string, PlayerNameStatsResponse>();

        public DataService(sc2dsstatsContext context, IMemoryCache memoryCache, IHttpClientFactory clientFactory, IServiceScopeFactory scopeFactory, ILogger<DataService> logger)
        {
            this.context = context;
            this.memoryCache = memoryCache;
            this.clientFactory = clientFactory;
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        private async Task<List<CmdrStats>> GetStats(bool player)
        {
            List<CmdrStats> stats;
            string memkey = player ? "cmdrstatsplayer" : "cmdrstats";
            if (!memoryCache.TryGetValue(memkey, out stats))
            {
                stats = await StatsService.GetStats(context, player);
                memoryCache.Set(memkey, stats, CacheService.RankingCacheOptions);
            }
            return stats;
        }

        private async Task<double> GetLeaver(DsRequest request)
        {
            double leaver;

            //string memKey = "leaver" + request.StartTime.ToString("yyyyMMdd") + request.EndTime.ToString("yyyyMMdd");
            //if (!memoryCache.TryGetValue(memKey, out leaver))
            //{
            //    leaver = await StatsService.GetLeaver(context, request);
            //    memoryCache.Set(memKey, leaver, CacheService.RankingCacheOptions);
            //}

            leaver = await StatsService.GetLeaver(context, request);
            return leaver;
        }

        private async Task<double> GetQuits(DsRequest request)
        {
            double leaver;
            //string memKey = "quits" + request.StartTime.ToString("yyyyMMdd") + request.EndTime.ToString("yyyyMMdd");
            //if (!memoryCache.TryGetValue(memKey, out leaver))
            //{
            //    leaver = await StatsService.GetQuits(context, request);
            //    memoryCache.Set(memKey, leaver, CacheService.RankingCacheOptions);
            //}

            leaver = await StatsService.GetQuits(context, request);
            return leaver;
        }

        private async Task<DsCountResponse> GetCountResponse(DsRequest request)
        {
            return await StatsService.GetCount(context, request);
        }

        public async Task<DsResponse> LoadData(DsRequest request)
        {
            DsResponse response;
            if (request.Filter == null || request.Filter.isDefault)
            {
                response = request.Mode switch
                {
                    "Winrate" => StatsService.GetWinrate(request, await GetStats(request.Player)),
                    "Timeline" => StatsService.GetTimeline(new TimelineRequest(request.Mode, request.Timespan, request.Player, request.Interest, request.Versus)
                    , await GetStats(request.Player)),
                    "MVP" => request.Player ? await StatsService.GetCustomMvp(request, context) : StatsService.GetMvp(request, await GetStats(request.Player)),
                    "DPS" => StatsService.GetDps(request, await GetStats(request.Player)),
                    "Synergy" => await StatsService.GetSynergy(context, request),
                    "AntiSynergy" => await StatsService.GetAntiSynergy(context, request),
                    "Duration" => await StatsService.GetDuration(context, request),
                    "Standard" => await StatsService.GetStandardTeamWinrate(request, context),
                    _ => StatsService.GetWinrate(request, await GetStats(request.Player))
                };
            }
            else
            {
                response = request.Mode switch
                {
                    "Winrate" => await StatsService.GetCustomWinrate(request, context),
                    "Timeline" => await StatsService.GetCustomTimeline(context, new TimelineRequest(request.Mode, request.Timespan, request.Player, request.Interest, request.Versus)),
                    "MVP" => await StatsService.GetCustomMvp(request, context),
                    "DPS" => await StatsService.GetCustomDps(request, context),
                    "Synergy" => await StatsService.GetSynergy(context, request),
                    "AntiSynergy" => await StatsService.GetAntiSynergy(context, request),
                    "Duration" => await StatsService.GetDuration(context, request),
                    "Standard" => await StatsService.GetStandardTeamWinrate(request, context),
                    _ => await StatsService.GetCustomWinrate(request, context),
                };
            }
            response.CountResponse = await GetCountResponse(request);
            return response;
        }

        private async Task UpdateNames()
        {
            var Http = clientFactory.CreateClient("sc2dsstats.app");
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();

                    var info = await context.DsInfo.FirstAsync();
                    var response = await Http.GetAsync($"secure/data/unitnames/{info.UnitNamesUpdate.ToString("yyyy-MM-dd")}");
                    if (response.IsSuccessStatusCode)
                    {
                        var unitnames = await response.Content.ReadFromJsonAsync<List<NameResponse>>();
                        await context.Database.ExecuteSqlRawAsync("DELETE FROM unitnames");
                        context.UnitNames.AddRange(unitnames.Select(s => new UnitName()
                        {
                            sId = s.sId,
                            Name = s.Name
                        }));
                        info.UnitNamesUpdate = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                    }

                    response = await Http.GetAsync($"secure/data/upgradenames/{info.UpgradeNamesUpdate.ToString("yyyy-MM-dd")}");
                    if (response.IsSuccessStatusCode)
                    {
                        var upgradenames = await response.Content.ReadFromJsonAsync<List<NameResponse>>();
                        await context.Database.ExecuteSqlRawAsync("DELETE FROM upgradenames");
                        context.UpgradeNames.AddRange(upgradenames.Select(s => new UpgradeName()
                        {
                            sId = s.sId,
                            Name = s.Name
                        }));
                        info.UpgradeNamesUpdate = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"name update failed: {ex.Message}");
            }
        }

        public async Task<bool> UploadData()
        {
            await UpdateNames();
            var Http = clientFactory.CreateClient("sc2dsstats.app");

            using (var scope = scopeFactory.CreateScope())
            {
                var uploadContext = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                var replayService = scope.ServiceProvider.GetRequiredService<ReplayService>();
                var success = await UploadService.Upload(Http, uploadContext, replayService.AppConfig.Config, logger);
                if (success)
                {
                    replayService.SaveConfig();
                }
                return success;
            }
        }

        public async Task<bool> TestUpload(UserConfig config)
        {
            var Http = clientFactory.CreateClient("sc2dsstats.app");
            using (var scope = scopeFactory.CreateScope())
            {
                var uploadContext = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                var replayService = scope.ServiceProvider.GetRequiredService<ReplayService>();
                var success = await UploadService.Upload(Http, uploadContext, config, logger);
                if (success)
                {
                    replayService.SaveConfig();
                }
                return success;
            }
        }



        public async Task<List<DsReplayResponse>> GetReplays(DsReplayRequest request, CancellationToken cancellationToken)
        {
            return await sc2dsstats.db.Services.ReplayService.GetReplays(context, request, cancellationToken);
        }
        public async Task<int> GetReplaysCount(DsReplayRequest request)
        {
            return await sc2dsstats.db.Services.ReplayService.GetCount(context, request);
        }
        public async Task<DsGameResponse> GetReplay(string hash)
        {
            return await sc2dsstats.db.Services.ReplayService.GetReplay(context, hash);
        }
        public async Task<DsBuildResponse> GetBuild(DsBuildRequest request)
        {
            return await BuildService.GetBuild(context, request);
        }
        public async Task<List<DsRankingResponse>> GetRankings()
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetPlayernames()
        {
            List<string> names;
            if (!memoryCache.TryGetValue("playernames", out names))
            {
                names = await context.Dsplayers.Select(s => s.Name).Distinct().ToListAsync();
                memoryCache.Set("playernames", names, new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.Low)
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1)));
            }
            return names;
        }

        public async Task<DsPlayerStats> GetPlayerStats(List<string> playerNames)
        {
            return await StatsService.GetPlayerStats(context, playerNames);
        }

        public async Task<string> GetLeaverInfo(string Timespan)
        {
            DsRequest request = new DsRequest() { Filter = new DsFilter() };
            request.Filter.SetOff();
            request.SetTime(Timespan);
            request.Filter.GameModes = new List<int>() { (int)DSData.Gamemode.Commanders, (int)DSData.Gamemode.CommandersHeroic };

            var replays = ReplayFilter.Filter(context, request);

            var leaver = from r in replays
                         group r by r.Maxleaver > 89 into g
                         select new
                         {
                             Leaver = g.Key,
                             Count = g.Count()
                         };
            var lleaver = await leaver.ToListAsync();

            return lleaver[0].Count == 0 ? "0" : (lleaver[1].Count / (double)lleaver[0].Count * 100).ToString("00.00");
        }

        public async Task<List<PlayerNameResponse>> GetPlayerNameStats()
        {
            var stats = await PlayerNameService.GetPlayers(context);
            //for (int i = 1; i < Math.Min(stats.Count, 20) - 1; i++)
            //{
            //    stats[i].Stats = await PlayerNameService.GetPlayerStats(context, stats[i].Name);
            //}
            return stats;
        }

        public async Task<PlayerNameStatsResponse?> GetPlayerNameStatsResponse(string name, CancellationToken token)
        {
            try
            {
                PlayerNameStatsResponse? stats;
                if (PlayerStats.TryGetValue(name, out stats))
                {
                    return stats;
                }
                else
                {
                    stats = await PlayerNameService.GetPlayerStats(context, name, token);
                    if (stats != null)
                    {
                        PlayerStats.TryAdd(name, stats);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            return null;
        }

        public void ClearPlayerStats()
        {
            PlayerStats.Clear();
        }

        public async Task DeleteReplay(int replayId)
        {
            var replay = await context.Dsreplays
                .Include(i => i.Middles)
                .Include(i => i.Dsplayers)
                    .ThenInclude(j => j.Breakpoints)
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.Id == replayId);
            if (replay != null)
            {
                context.Dsreplays.Remove(replay);
                await context.SaveChangesAsync();
            }
        }
    }
}
