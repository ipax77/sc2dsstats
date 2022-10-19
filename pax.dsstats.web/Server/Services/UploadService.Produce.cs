using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using System.Text.Json;
using System.Threading.Channels;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    private Channel<ReplayDto> ReplayChannel = Channel.CreateUnbounded<ReplayDto>();
    private object lockobject = new();
    private bool insertJobRunning;
    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();

    public async Task Produce(string gzipbase64string, Guid appGuid)
    {
        var replays = JsonSerializer.Deserialize<List<ReplayDto>>(await UnzipAsync(gzipbase64string))?.OrderByDescending(o => o.GameTime).ToList();

        if (replays == null || !replays.Any())
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var uploader = await context
            .Uploaders.Include(i => i.Players)
            .FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        if (uploader == null)
        {
            return;
        }

        // replays.SelectMany(s => s.Players).Where(x => uploader.Players.Select(t => t.Name).Contains(x.Name)).ToList().ForEach(f => f.IsUploader = true);

        for (int i = 0; i < replays.Count; i++)
        {
            ReplayChannel.Writer.TryWrite(replays[i]);
        }

        _ = InsertReplays();
    }

    public async Task InsertReplays()
    {
        lock (lockobject)
        {
            if (insertJobRunning)
            {
                return;
            }
            insertJobRunning = true;
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = serviceProvider.GetRequiredService<IReplayRepository>();

        if (!Units.Any())
        {
            Units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        }
        if (!Upgrades.Any())
        {
            Upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();
        }

        while (await ReplayChannel.Reader.WaitToReadAsync())
        {
            if (ReplayChannel.Reader.TryRead(out ReplayDto? replayDto))
            {
                if (replayDto == null)
                {
                    continue;
                }

                var dupReplayExists = context.Replays.Any(f => f.ReplayHash == replayDto.ReplayHash);

                if (!dupReplayExists)
                {
                    await replayRepository.SaveReplay(replayDto, Units, Upgrades, null);
                }
                else
                {
                    if (await HandleDuplicate(context, replayDto))
                    {
                        await replayRepository.SaveReplay(replayDto, Units, Upgrades, null);
                    };
                }
            }
        }
        insertJobRunning = false;
    }

    public async Task<bool> HandleDuplicate(ReplayContext context, ReplayDto replayDto)
    {
        var dupReplay = await context.Replays.FirstOrDefaultAsync(f => f.ReplayHash == replayDto.ReplayHash);

        if (dupReplay == null)
        {
            return false;
        }

        if (dupReplay.GameTime - replayDto.GameTime > TimeSpan.FromDays(1))
        {
            logger.LogWarning($"false positive duplicate? {dupReplay.ReplayHash}");
            return false;
        }

        if (replayDto.Duration > dupReplay.Duration + 60)
        {
            var delReplay = await context.Replays
                .Include(i => i.Players)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.Players)
                    .ThenInclude(i => i.Upgrades)

                .FirstAsync(f => f.ReplayHash == replayDto.ReplayHash);

            context.Replays.Remove(delReplay);
            await context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
