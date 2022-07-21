using Microsoft.EntityFrameworkCore;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using System.Threading.Channels;

namespace sc2dsstats._2022.Server.Services;

public class InsertService : IInsertService
{
    private bool jobRunning = false;
    private object lockobject = new object();

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<InsertService> logger;
    private Channel<Dsreplay> ReplayChannel;
    private CancellationTokenSource tokenSource;
    public event EventHandler<InsertEventArgs> ReplaysInserted;

    protected virtual void OnReplaysInserted(InsertEventArgs e)
    {
        EventHandler<InsertEventArgs> handler = ReplaysInserted;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    public InsertService(IServiceScopeFactory scopeFactory, ILogger<InsertService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        Reset();
    }

    public void AddReplays(List<Dsreplay> replays)
    {
        _ = InsertReplays();
        for (int i = 0; i < replays.Count; i++)
        {
            AddReplay(replays[i]);
        }
    }

    public bool AddReplay(Dsreplay replay)
    {
        return ReplayChannel.Writer.TryWrite(replay);
    }

    private async Task InsertReplays()
    {
        lock (lockobject)
        {
            if (jobRunning)
            {
                return;
            }
            else
            {
                jobRunning = true;
                tokenSource = new CancellationTokenSource();
            }
        }
        logger.LogInformation($"consumer job working.");
        using (var scope = scopeFactory.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                while (await ReplayChannel.Reader.WaitToReadAsync(tokenSource.Token))
                {
                    Dsreplay replay;
                    if (ReplayChannel.Reader.TryRead(out replay))
                    {
                        await SetPlayerNames(replay, context);

                        if (!await CheckDuplicate(replay, context))
                        {
                            if (new Version(replay.Version) <= new Version(4, 0))
                            {
                                DbService.SetMid(replay);
                            }
                            context.Dsreplays.Add(replay);
                        }

                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                OnReplaysInserted(new InsertEventArgs());
                Reset();
            }
        }
        logger.LogInformation($"consumer job finished.");
    }

    private async Task<bool> CheckDuplicate(Dsreplay replay, sc2dsstatsContext context)
    {
        var dbReplay = await context.Dsreplays
            .Include(i => i.Dsplayers)
            .FirstOrDefaultAsync(f => f.Hash == replay.Hash);

        if (dbReplay == null)
        {
            return false;
        }

        foreach (var incPlayer in replay.Dsplayers.Where(x => x.isPlayer))
        {
            var exPlayer = dbReplay.Dsplayers.FirstOrDefault(f => f.Realpos == incPlayer.Realpos);
            if (exPlayer != null && exPlayer.PlayerName == null)
            {
                exPlayer.PlayerName = incPlayer.PlayerName;
                exPlayer.Name = incPlayer.PlayerName.DbId.ToString();
            }
        }

        if (new Version(dbReplay.Version) <= new Version(4, 0)
            && new Version(replay.Version) > new Version(4, 0))
        {
            dbReplay.Bunker = replay.Bunker;
            dbReplay.Cannon = replay.Cannon;
            dbReplay.Mid1 = replay.Mid1;
            dbReplay.Mid2 = replay.Mid2;
        }

        return true;
    }

    private async Task SetPlayerNames(Dsreplay replay, sc2dsstatsContext context)
    {
        foreach (var player in replay.Dsplayers.Where(x => x.isPlayer))
        {
            DsPlayerName playerName;
            if (player.Name.Length == 64)
            {
                playerName = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Hash == player.Name);
            }
            else
            {
                playerName = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.AppId.ToString() == player.Name);
            }
            if (playerName == null)
            {
                continue;
            }
            player.Name = playerName.DbId.ToString();
            player.PlayerName = playerName;
            if (!playerName.NamesMapped && playerName.AppId != Guid.Empty && !String.IsNullOrEmpty(playerName.Hash))
            {
                var mapReps = await context.Dsplayers.Where(x => x.Name == playerName.Hash).ToListAsync();
                foreach (var pl in mapReps)
                {
                    pl.Name = playerName.DbId.ToString();
                    pl.PlayerName = playerName;
                }
                playerName.NamesMapped = true;
            }
        }
    }

    public void WriteStart()
    {
        _ = InsertReplays();
    }

    public void WriteFinished()
    {
        ReplayChannel.Writer.Complete();
    }

    public void Reset()
    {
        jobRunning = false;
        tokenSource?.Cancel();
        ReplayChannel = Channel.CreateUnbounded<Dsreplay>();
        tokenSource = new CancellationTokenSource();
    }

    public void Cancel()
    {
        tokenSource.Cancel();
        tokenSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
        tokenSource.Cancel();
        tokenSource.Dispose();
        ReplayChannel = null;
    }
}
