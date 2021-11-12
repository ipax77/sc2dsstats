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
    public class DsController : ControllerBase
    {
        private readonly sc2dsstatsContext context;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<DsController> logger;

        public DsController(sc2dsstatsContext context, IMemoryCache memoryCache, ILogger<DsController> logger)
        {
            this.context = context;
            this.memoryCache = memoryCache;
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
            }
            return stats;
        }

        [HttpPost]
        [Route("timeline")]
        public async Task<ActionResult<TimelineResponse>> GetTimeline(TimelineRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            TimelineResponse response;
            if (request.Filter == null || request.Filter.isDefault)
                response = await TimelineService.GetTimelineFromTimeResults(context, request);
            else
                response = await TimelineService.GetTimeline(context, request);
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
            var response = await DurationService.GetDuration(context, request);
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
                response = await WinrateService.GetWinrateFromTimeResults(context, request);
            else
                response = await WinrateService.GetWinrate(context, request);
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
                response = await MvpService.GetMvpFromTimeResults(context, request);
            else
                response = await MvpService.GetMvp(context, request);
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
                response = await DpsService.GetDpsFromTimeResults(context, request);
            else
                response = await DpsService.GetDps(context, request);
            sw.Stop();
            logger.LogInformation($"Get Mvp in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("synergy")]
        public async Task<ActionResult<DsResponse>> GetSynergy(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // var response = await WinrateService.GetWinrate(request, context);
            var response = await SynergyService.GetSynergy(context, request);
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
            // var response = await WinrateService.GetWinrate(request, context);
            var response = await SynergyService.GetAntiSynergy(context, request);
            sw.Stop();
            logger.LogInformation($"Get AntiSynergy in {sw.ElapsedMilliseconds} ms");
            return response;
        }

        [HttpPost]
        [Route("crosstable")]
        public async Task<ActionResult<DsResponse>> GetCrosstable(DsRequest request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // var response = await CrossTableService.GetCrossTableData(request, context);
            var response = await CrossTableService.GetCrosstableFromTimeResults(context, request);
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
    }
}
