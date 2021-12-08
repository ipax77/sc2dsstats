using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sc2dsstats._2022.Shared;

namespace sc2dsstats.db.Services
{
    public class CacheService
    {
        private readonly IMemoryCache memoryCache;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<CacheService> logger;
        public static MemoryCacheEntryOptions BuildCacheOptions;
        public static MemoryCacheEntryOptions RankingCacheOptions;
        public static int Size = 0;
        private HashSet<string> HashKeys = new HashSet<string>();
        private object lockobject = new object();
        public bool Updatesavailable { get; set; } = false;

        public CacheService(IMemoryCache memoryCache, IServiceScopeFactory scopeFactory, ILogger<CacheService> logger)
        {
            this.memoryCache = memoryCache;
            this.scopeFactory = scopeFactory;
            this.logger = logger;

            BuildCacheOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.High)
                .SetAbsoluteExpiration(TimeSpan.FromDays(7));
            RankingCacheOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.High)
                .SetAbsoluteExpiration(TimeSpan.FromDays(1));
        }

        public void AddHashKey(string key)
        {
            lock (lockobject)
            {
                HashKeys.Add(key);
            }
        }

        public void ResetCache(bool force = false)
        {
            lock (lockobject)
            {
                if (Updatesavailable || force)
                {
                    for (int i = 0; i < HashKeys.Count; i++)
                    {
                        memoryCache.Remove(HashKeys.ElementAt(i));
                    }
                }
            }
        }

        public async Task SetBuildCache()
        {
            Size = 0;
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<sc2dsstatsContext>();

                var request = new DsBuildRequest()
                {
                    Playername = "PAX",
                };
                request.SetTime("Patch 2.60");

                foreach (var cmdr in DSData.cmdrs)
                {
                    request.Interest = cmdr;
                    request.Versus = String.Empty;

                    var result = await BuildService.GetBuild(context, request);
                    memoryCache.Set(request.CacheKey, result, BuildCacheOptions);
                    Size++;

                    foreach (var vs in DSData.cmdrs)
                    {
                        request.Versus = vs;
                        result = await BuildService.GetBuild(context, request);
                        memoryCache.Set(request.CacheKey, result, BuildCacheOptions);
                        Size++;
                    }
                }
            }
            logger.LogInformation($"Build cache set. Now at {Size} items.");
        }
    }
}
