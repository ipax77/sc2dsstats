using Microsoft.EntityFrameworkCore;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using System.Threading.Channels;

namespace sc2dsstats.app.Services
{
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
                int i = 0;
                HashSet<DsPlayerName> playerCache = new HashSet<DsPlayerName>();
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    while (await ReplayChannel.Reader.WaitToReadAsync(tokenSource.Token))
                    {
                        Dsreplay replay;
                        if (ReplayChannel.Reader.TryRead(out replay))
                        {
                            if (!await context.Dsreplays.AnyAsync(a => a.Hash == replay.Hash))
                            {
                                HashSet<string> names = new HashSet<string>();
                                foreach (var pl in replay.Dsplayers)
                                {
                                    if (names.Contains(pl.Name))
                                        continue;
                                    var player = playerCache.FirstOrDefault(f => f.Name == pl.Name);
                                    if (player == null)
                                    {
                                        player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Name == pl.Name);
                                    }
                                    if (player == null)
                                    {
                                        player = new DsPlayerName()
                                        {
                                            Name = pl.Name,
                                        };
                                        names.Add(pl.Name);
                                        playerCache.Add(player);
                                    }
                                    pl.PlayerName = player;
                                }
                                context.Dsreplays.Add(replay);
                            }
                        }
                        if (i % 1000 == 0)
                        {
                            await context.SaveChangesAsync();
                            OnReplaysInserted(new InsertEventArgs()
                            {
                                insertCount = i
                            });
                        }
                        i++;
                    }
                    await context.SaveChangesAsync();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
                finally
                {

                    OnReplaysInserted(new InsertEventArgs()
                    {
                        insertCount = i,
                        Done = true
                    });
                    Reset();
                }
            }
            logger.LogInformation($"consumer job finished.");
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
}
