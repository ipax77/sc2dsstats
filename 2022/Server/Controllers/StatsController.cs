using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using sc2dsstats._2022.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using sc2dsstats.db.Stats;

namespace sc2dsstats._2022.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly sc2dsstatsContext context;
        private readonly IMemoryCache memoryCache;
        private readonly CacheService cacheService;
        private readonly ILogger<StatsController> logger;

        public StatsController(sc2dsstatsContext context, IMemoryCache memoryCache, CacheService cacheService, ILogger<StatsController> logger)
        {
            this.context = context;
            this.memoryCache = memoryCache;
            this.cacheService = cacheService;
            this.logger = logger;
        }

        private async Task<List<CmdrStats>> GetCmdrStats(bool player)
        {
            string memoryKey = player ? "cmdrstatsplayer" : "cmdrstats";
            List<CmdrStats> stats;
            if (!memoryCache.TryGetValue(memoryKey, out stats))
            {
                stats = await StatsService.GetStats(context, player);
                memoryCache.Set(memoryKey, stats, CacheService.RankingCacheOptions);
                cacheService.AddHashKey(memoryKey);
            }
            return stats;
        }

        private async Task<double> GetLeaver(DsRequest request)
        {
            double leaver;
            string memKey = "leaver" + request.StartTime.ToString("yyyyMMdd") + request.EndTime.ToString("yyyyMMdd");
            if (!memoryCache.TryGetValue(memKey, out leaver))
            {
                leaver = await StatsService.GetLeaver(context, request);
                memoryCache.Set(memKey, leaver, CacheService.RankingCacheOptions);
            }
            return leaver;
        }

        private async Task<double> GetQuits(DsRequest request)
        {
            double leaver;
            string memKey = "quits" + request.StartTime.ToString("yyyyMMdd") + request.EndTime.ToString("yyyyMMdd");
            if (!memoryCache.TryGetValue(memKey, out leaver))
            {
                leaver = await StatsService.GetQuits(context, request);
                memoryCache.Set(memKey, leaver, CacheService.RankingCacheOptions);
            }
            return leaver;
        }

        private async Task<DsCountResponse> GetCountResponse(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string hash = request.GenHash();
            DsCountResponse response;
            string memKey = "countresponse" + hash;
            if (!memoryCache.TryGetValue(memKey, out response))
            {
                response = await StatsService.GetCount(context, request);
                memoryCache.Set(memKey, response, CacheService.RankingCacheOptions);
                cacheService.AddHashKey(memKey);
            }
            sw.Stop();
            logger.LogInformation($"Get count response in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        private async Task SetLeaverQuit(DsRequest request, DsResponse response)
        {
            response.CountResponse = await GetCountResponse(request);
        }

        [HttpGet]
        public async Task<ActionResult<Dictionary<string, double>>> Get()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Dictionary<string, double> Counts = new Dictionary<string, double>();
            foreach (var cmdr in DSData.GetCommanders)
            {
                double count = await context.Dsplayers.Where(x => x.Race == (byte)cmdr).CountAsync();
                double wins = await context.Dsplayers.Where(x => x.Race == (byte)cmdr && x.Win == true).CountAsync();
                //double count = await context.Dsplayersfiltereds.Where(x => x.Race == cmdr).CountAsync();
                //double wins = await context.Dsplayersfiltereds.Where(x => x.Race == cmdr && x.Win == true).CountAsync();
                Counts[cmdr.ToString()] = Math.Round(wins * 100 / count, 2);
            }
            sw.Stop();
            logger.LogInformation($"Get in {sw.ElapsedMilliseconds} ms");
            return Counts;
        }

        [HttpPost]
        [Route("timeline")]
        public async Task<ActionResult<TimelineResponse>> GetTimeline(TimelineRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            TimelineResponse response;
            if (request.Filter == null || request.Filter.isDefault)
            {
                string hash = request.GenHash();
                if (!memoryCache.TryGetValue(hash, out response))
                {
                    response = StatsService.GetTimeline(request, await GetCmdrStats(request.Player));
                    memoryCache.Set(hash, response, CacheService.RankingCacheOptions);
                }
                else
                    logger.LogInformation("timeline from cache");
            }
            else
                response = await StatsService.GetCustomTimeline(context, request);
            
            await SetLeaverQuit(request, response);
            sw.Stop();
            logger.LogInformation($"Get Timeline in {sw.ElapsedMilliseconds} ms");
            
            return response;
        }

        [HttpPost]
        [Route("duration")]
        public async Task<ActionResult<TimelineResponse>> GetDuration(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            TimelineResponse response;
            // var response = await DurationService.GetDuration(context, request);
            if (request.Filter == null || request.Filter.isDefault)
            {
                string hash = request.GenHash();
                if (!memoryCache.TryGetValue(hash, out response))
                {
                    response = await StatsService.GetDuration(context, request);
                    memoryCache.Set(hash, response, CacheService.BuildCacheOptions);
                }
                else
                    logger.LogInformation("duration from cache");
            }
            else
                response = await StatsService.GetDuration(context, request);
            
            await SetLeaverQuit(request, response);
            sw.Stop();
            logger.LogInformation($"Get Timeline in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("winrate")]
        public async Task<ActionResult<DsResponse>> GetWinrate(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DsResponse response;
            if (request.Filter == null || request.Filter.isDefault)
            {
                // response = await WinrateService.GetWinrateFromTimeResults(context, request);
                response = StatsService.GetWinrate(request, await GetCmdrStats(request.Player));
            }
            else
            {
                // response = await WinrateService.GetWinrate(context, request);
                response = await StatsService.GetCustomWinrate(request, context);
            }
            await SetLeaverQuit(request, response);
            sw.Stop();
            logger.LogInformation($"Get Winrate in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("mvp")]
        public async Task<ActionResult<DsResponse>> GetMvp(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DsResponse response;
            if (request.Filter == null || request.Filter.isDefault)
            {
                // response = await WinrateService.GetWinrateFromTimeResults(context, request);
                response = StatsService.GetMvp(request, await GetCmdrStats(request.Player));
            }
            else
            {
                // response = await WinrateService.GetWinrate(context, request);
                response = await StatsService.GetCustomMvp(request, context);
            }
            await SetLeaverQuit(request, response);
            sw.Stop();
            logger.LogInformation($"Get Mvp in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("dps")]
        public async Task<ActionResult<DsResponse>> GetDps(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DsResponse response;
            if (request.Filter == null || request.Filter.isDefault)
            {
                // response = await WinrateService.GetWinrateFromTimeResults(context, request);
                response = StatsService.GetDps(request, await GetCmdrStats(request.Player));
            }
            else
            {
                // response = await WinrateService.GetWinrate(context, request);
                response = await StatsService.GetCustomDps(request, context);
            }
            await SetLeaverQuit(request, response);
            sw.Stop();
            logger.LogInformation($"Get Dps in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("synergy")]
        public async Task<ActionResult<DsResponse>> GetSynergy(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DsResponse response;
            if (request.Filter == null || request.Filter.isDefault)
            {
                string hash = request.GenHash();
                if (!memoryCache.TryGetValue(hash, out response))
                {
                    response = await StatsService.GetSynergy(context, request);
                    memoryCache.Set(hash, response, CacheService.BuildCacheOptions);
                }
                else
                    logger.LogInformation("synergy from cache");
            }
            else
                response = await StatsService.GetSynergy(context, request);

            await SetLeaverQuit(request, response);
            sw.Stop();
            logger.LogInformation($"Get Synergy in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("antisynergy")]
        public async Task<ActionResult<DsResponse>> GetAntiSynergy(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DsResponse response;
            if (request.Filter == null || request.Filter.isDefault)
            {
                string hash = request.GenHash();
                if (!memoryCache.TryGetValue(hash, out response))
                {
                    response = await StatsService.GetAntiSynergy(context, request);
                    memoryCache.Set(hash, response, CacheService.BuildCacheOptions);
                }
                else
                    logger.LogInformation("antisynergy from cache");
            }
            else
                response = await StatsService.GetAntiSynergy(context, request);

            await SetLeaverQuit(request, response);
            sw.Stop();
            logger.LogInformation($"Get AntiSynergy in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("teamstandard")]
        public async Task<ActionResult<DsResponse>> GetStandardTeam(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DsResponse response;
            string hash = request.GenHash();
            if (!memoryCache.TryGetValue(hash, out response))
            {
                response = await StatsService.GetStandardTeamWinrate(request, context);
                memoryCache.Set(hash, response, CacheService.BuildCacheOptions);
            }
            sw.Stop();
            logger.LogInformation($"Get StandardTeam in {sw.ElapsedMilliseconds} ms");

            response.CountResponse = new DsCountResponse()
            {
                FilteredCount = response.Count
            };

            return response;
        }

        [HttpPost]
        [Route("crosstable")]
        public async Task<ActionResult<DsResponse>> GetCrosstable(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var response = StatsService.GetCrosstable(request, await GetCmdrStats(request.Player));
            sw.Stop();
            logger.LogInformation($"Get Crosstable in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("build")]
        public async Task<ActionResult<DsBuildResponse>> GetBuild(DsBuildRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DsBuildResponse response;
            if (!memoryCache.TryGetValue(request.CacheKey, out response))
            {
                response = await BuildService.GetBuild(context, request);
                memoryCache.Set(request.CacheKey, response, CacheService.BuildCacheOptions);
                sw.Stop();
                logger.LogInformation($"Get Build in {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                sw.Stop();
                logger.LogInformation($"Get Build from Cache in {sw.ElapsedMilliseconds} ms");
            }
            return response;
        }

        [HttpGet]
        [Route("ranking")]
        public async Task<ActionResult<List<DsRankingResponse>>> GetRanking()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<DsRankingResponse> rankings;
            if (!memoryCache.TryGetValue("Ranking", out rankings))
            {
                rankings = await RankingService.GetRanking(context);
                memoryCache.Set("Ranking", rankings, CacheService.BuildCacheOptions);
                sw.Stop();
                logger.LogInformation($"Got Rankings in {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                sw.Stop();
                logger.LogInformation($"Got Rankings from Cache in {sw.ElapsedMilliseconds} ms");
            }
            return rankings;
        }

        [HttpGet]
        [Route("names")]
        public async Task<List<string>> GetPlayernames()
        {
            List<string> names;
            if (!memoryCache.TryGetValue("Playernames", out names))
            {
                names = await context.Dsplayers.Select(s => s.Name.Length > 13 ? s.Name.Substring(0, 12) : s.Name).Distinct().ToListAsync();
                memoryCache.Set("Playernames", names, CacheService.RankingCacheOptions);
            }
            return names;
        }

        [HttpGet]
        [Route("playerstats/{name}")]
        public async Task<ActionResult<DsPlayerStats>> GetPlayerStats(string name)
        {
            try
            {
                var playerStats = await StatsService.GetPlayerStats(context, name);
                if (playerStats == null)
                    return NotFound();
                else
                    return playerStats;
            } catch (Exception e)
            {
                logger.LogError($"failed getting player stats {name}: {e.Message}");
                return StatusCode(500);
            }
        }
    }
}
