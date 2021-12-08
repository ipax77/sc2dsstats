using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using System.IO.Compression;
using System.Text.Json;

namespace sc2dsstats._2022.Server.Services;

public class ProducerService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ProducerService> logger;
    public int NewReplaysCount { get; set; } = 0;

    public ProducerService(IServiceScopeFactory scopeFactory, ILogger<ProducerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task<DateTime?> Produce(string gzipbase64String, Guid appid)
    {
        try
        {
            var replays = JsonSerializer.Deserialize<List<DsReplayDto>>(await DSData.UnzipAsync(gzipbase64String)).OrderByDescending(o => o.GAMETIME);

            if (replays.Any())
            {
                var dsreplays = replays.Select(s => new Dsreplay(s)).ToList();
                dsreplays.SelectMany(s => s.Dsplayers).Where(x => x.Name == "player").ToList().ForEach(f => { f.Name = appid.ToString(); f.isPlayer = true; });
                ReplayFilter.SetDefaultFilter(dsreplays);
                Produce(dsreplays);
                return dsreplays.First().Gametime;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed producing data: {ex.Message}");
        }
        return null;
    }

    public async Task<DateTime?> Produce(string gzipbase64String, string nameHash)
    {
        try
        {
            var replays = JsonSerializer.Deserialize<List<DsReplayDto>>(await DSData.UnzipAsync(gzipbase64String)).OrderByDescending(o => o.GAMETIME);

            if (replays.Any())
            {
                var dsreplays = replays.Select(s => new Dsreplay(s)).ToList();
                dsreplays.SelectMany(s => s.Dsplayers).Where(x => x.Name == "player").ToList().ForEach(f => { f.Name = nameHash; f.isPlayer = true; });
                ReplayFilter.SetDefaultFilter(dsreplays);
                Produce(dsreplays);
                return dsreplays.First().Gametime;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed producing data: {ex.Message}");
        }
        return null;
    }

    public async Task<DateTime?> Produce(IFormFile file, string nameHash)
    {
        List<DsReplayDto> replays;
        using (var stream = new MemoryStream((int)file.Length))
        {
            await file.CopyToAsync(stream);
            replays = await Decompress(stream);
        }

        if (replays.Any())
        {
            var dsreplays = replays.Select(s => new Dsreplay(s)).ToList();
            dsreplays.SelectMany(s => s.Dsplayers).Where(x => x.Name == "player").ToList().ForEach(f => { f.Name = nameHash; f.isPlayer = true; });
            ReplayFilter.SetDefaultFilter(dsreplays);
            Produce(dsreplays);
            return dsreplays.First().Gametime;
        }
        return null;
    }

    private void Produce(List<Dsreplay> replays)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var insertService = scope.ServiceProvider.GetRequiredService<IInsertService>();
            var cacheService = scope.ServiceProvider.GetRequiredService<CacheService>();
            cacheService.Updatesavailable = true;
            insertService.AddReplays(replays);
        }
    }

    private async Task<List<DsReplayDto>> Decompress(MemoryStream stream)
    {
        List<DsReplayDto> replays = new List<DsReplayDto>();
        try
        {
            stream.Position = 0;
            using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                using (StreamReader sr = new StreamReader(decompressionStream))
                {
                    string line;
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        var replay = JsonSerializer.Deserialize<DsReplayDto>(line, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                        if (replay != null)
                            replays.Add(replay);
                    }
                }

            }

        }
        catch (Exception e)
        {
            logger.LogError($"Failed decompressing: {e.Message}");
        }
        return replays.OrderByDescending(o => o.GAMETIME).ToList();
    }
}
