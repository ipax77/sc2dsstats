using AutoMapper;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.parser;
using pax.dsstats.shared;
using System.Diagnostics;
using System.Reflection;
using s2protocol.NET;
using pax.dsstats.dbng.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace sc2dsstats.maui.Services;

public class DecodeService : IDisposable
{
    public DecodeService(ILogger<DecodeService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;

        decoderOptions = new ReplayDecoderOptions()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };
        var _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        decoder = new ReplayDecoder(_assemblyPath);
    }

    public int NewReplays { get; private set; }
    public int DbReplays { get; private set; }


    private readonly ILogger<DecodeService> logger;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ReplayDecoderOptions decoderOptions;
    private readonly ReplayDecoder decoder;
    private ConcurrentBag<string> errorReplays = new();

    private SemaphoreSlim semaphoreSlim = new(1, 1);
    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();

    private int decodeCounter;
    private int dbCounter;
    private int total;
    private int errorCounter;
    private DateTime startTime = DateTime.UtcNow;

    private object lockobject = new object();

    public bool IsRunning { get; private set; }

    public event EventHandler<DecodeEventArgs>? DecodeStateChanged;
    protected virtual void OnDecodeStateChanged(DecodeEventArgs e)
    {
        EventHandler<DecodeEventArgs>? handler = DecodeStateChanged;
        handler?.Invoke(this, e);
    }

    public ICollection<string> GetErrorReplays()
    {
        return errorReplays.ToArray();
    }

    public async Task<ReplayDto?> Decode(string replayPath)
    {
        // var replayPath = @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (4716).SC2Replay";

        var _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        Stopwatch sw = new Stopwatch();
        sw.Start();
        var sc2Replay = await Parse.GetSc2Replay(replayPath, _assemblyPath);

        var dsReplay = Parse.GetDsReplay(sc2Replay);

        sw.Stop();
        if (dsReplay == null)
        {
            logger.DecodeError($"dsReplay {replayPath} was NULL");
            return null;
        }

        var replayDto = Parse.GetReplayDto(dsReplay);

        if (replayDto == null)
        {
            return null;
        }

        await SaveReplay(replayDto);


        logger.DecodeInformation($"Got dsReplay: {dsReplay.GameMode} {dsReplay.Duration} in {sw.ElapsedMilliseconds} ms");
        return replayDto;
    }

    public async Task DecodeParallel(CancellationToken cancellationToken = default)
    {
        lock (lockobject)
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;
        }

        decodeCounter = 0;
        dbCounter = 0;
        errorReplays.Clear();
        errorCounter = 0;

        var replays = await ScanForNewReplays();
        total = replays.Count;
        startTime = DateTime.UtcNow;

        CancellationTokenSource cts = new();
        _ = Notify(cts.Token);

        Stopwatch sw = Stopwatch.StartNew();

        await foreach (var sc2rep in decoder.DecodeParallel(replays, UserSettingsService.UserSettings.CpuCoresUsedForDecoding, decoderOptions, cancellationToken))
        {
            try
            {
                var dsRep = Parse.GetDsReplay(sc2rep);
                if (dsRep != null)
                {
                    var dtoRep = Parse.GetReplayDto(dsRep);
                    if (dtoRep != null)
                    {
                        Interlocked.Increment(ref decodeCounter);
                        _ = SaveReplay(dtoRep);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.DecodeError($"failed parsing replay {sc2rep.FileName}: {ex.Message}");
                errorReplays.Add(sc2rep.FileName);
                Interlocked.Increment(ref errorCounter);
            }

            if (decodeCounter % 100 == 0)
            {
                logger.DecodeInformation($"replays decoded: {decodeCounter}/{total}, replays in db: {dbCounter}/{total}");
            }
        }

        sw.Stop();

        logger.DecodeInformation($"Got dsReplays in {sw.ElapsedMilliseconds} ms");

        await ScanForNewReplays();

        cts.Cancel();

        IsRunning = false;
    }

    private async Task Notify(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            OnDecodeStateChanged(new()
            {
                Start = startTime,
                Total = total,
                Decoded = decodeCounter,
                Error = errorCounter,
                Saved = dbCounter,
            });
            try
            {
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException) { }
        }

        OnDecodeStateChanged(new()
        {
            Start = startTime,
            Total = total,
            Decoded = decodeCounter,
            Error = errorCounter,
            Saved = dbCounter,
            Done = true
        });
    }

    internal async Task<List<string>> ScanForNewReplays()
    {
        Stopwatch sw = new();
        sw.Start();

        using var scope = serviceScopeFactory.CreateScope();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        var dbReplayPaths = await replayRepository.GetReplayPaths();
        var hdReplayPaths = GetHdReplayPaths();

        var newReplays = hdReplayPaths.Except(dbReplayPaths).ToList();

        sw.Stop();
        logger.DecodeInformation($"got new replays list (db: {dbReplayPaths.Count}, hd: {hdReplayPaths.Count} => {newReplays.Count}) in {sw.ElapsedMilliseconds} ms");

        NewReplays = newReplays.Count;
        DbReplays = dbReplayPaths.Count;
        return newReplays;
    }

    private ICollection<string> GetHdReplayPaths()
    {
        List<string> fileNames = new();
        foreach (var dir in UserSettingsService.UserSettings.ReplayPaths)
        {
            fileNames.AddRange(Directory.GetFiles(dir, $"{UserSettingsService.UserSettings.ReplayStartName}*.SC2Replay", SearchOption.AllDirectories));
        }
        return fileNames;
    }

    private async Task SaveReplay(ReplayDto replayDto)
    {
        await semaphoreSlim.WaitAsync();
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            if (!Units.Any())
            {
                Units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
            }

            if (!Upgrades.Any())
            {
                Upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();
            }

            var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
            (Units, Upgrades) = await replayRepository.SaveReplay(replayDto, Units, Upgrades, null);

            Interlocked.Increment(ref dbCounter);
        }
        catch (Exception ex)
        {
            logger.DecodeError($"failed saving replay: {ex.Message}");
            errorReplays.Add(replayDto.FileName);
            Interlocked.Increment(ref errorCounter);
        }
        finally
        {
            if (!IsRunning)
            {
                OnDecodeStateChanged(new()
                {
                    Start = startTime,
                    Total = total,
                    Decoded = decodeCounter,
                    Saved = dbCounter,
                    Done = true
                });
            }
            semaphoreSlim.Release();
        }


    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        decoder.Dispose();
    }
}

public class DecodeEventArgs : EventArgs
{
    public DateTime Start { get; set; }
    public int Total { get; set; }
    public int Decoded { get; set; }
    public int Error { get; set; }
    public int Saved { get; set; }
    public bool Done { get; set; }
}
