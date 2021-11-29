using sc2dsstats._2022.Shared;
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

    public async Task<List<DsReplayDto>> Decompress(MemoryStream stream)
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

    public void AddReplay(DsReplayDto replay)
    {
        CheckDuplicate(replay);
        PrepareReplay(replay);
    }

    private void PrepareReplay(DsReplayDto replay)
    {

    }

    private void CheckDuplicate(DsReplayDto replay)
    {

    }

}
