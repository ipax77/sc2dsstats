using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using sc2dsstats.db;
using sc2dsstats.lib.Db;
using sc2dsstats.db.Services;
using Microsoft.Extensions.Caching.Memory;
using sc2dsstats.db.Stats;

namespace sc2dsstats._2022.Server.Services
{
    public class UpdateService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<UpdateService> _logger;
        private readonly IServiceProvider _sp;
        private Timer _timer;

        public UpdateService(ILogger<UpdateService> logger, IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {

            _timer = new Timer(DoWork, null, new TimeSpan(0, 0, 4), new TimeSpan(1, 0, 0));
            // _timer = new Timer(DoWork, null, TimeSpan.Zero, new TimeSpan(24, 0, 0));
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);
            var acount = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (var scope = _sp.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                var oldcontext = scope.ServiceProvider.GetRequiredService<DSReplayContext>();
                var restcontext = scope.ServiceProvider.GetRequiredService<DSRestContext>();
                var insertService = scope.ServiceProvider.GetRequiredService<InsertService>();
                try
                {
                    // int repCount = OldContextsService.UpdateFromOldDb(context, oldcontext, insertService);
                    int repCount = 0;
                    var cacheService = scope.ServiceProvider.GetRequiredService<CacheService>();
                    if (repCount > 0)
                    {
                        insertService.NewReplaysCount = 0;
                        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                        
                        cacheService.ResetCache(true);
                        var stats = await StatsService.GetStats(context, false);
                        memoryCache.Set("cmdrstats", stats, CacheService.RankingCacheOptions);
                        var plstats = await StatsService.GetStats(context, true);
                        memoryCache.Set("cmdrstatsplayer", plstats, CacheService.RankingCacheOptions);
                    } else
                    {
                        cacheService.ResetCache();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Update Service failed: {e.Message}");
                }
            }
            sw.Stop();
            _logger.LogInformation(
                $"Update Service is working ({count}). Count: {acount} in {sw.Elapsed.TotalSeconds} s");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Update Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
