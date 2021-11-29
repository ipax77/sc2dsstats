using Microsoft.EntityFrameworkCore;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using System.Threading.Channels;

namespace sc2dsstats.app.Services
{
    public class InsertService2 : IInsertService
    {

        private bool jobRunning = false;
        private object lockobject = new object();

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<InsertService2> logger;
        private Channel<Dsreplay> ReplayChannel;
        private CancellationTokenSource tokenSource;
        public event EventHandler<EventArgs> ReplaysInserted;

        protected virtual void OnReplaysInserted(EventArgs e)
        {
            EventHandler<EventArgs> handler = ReplaysInserted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public InsertService2(IServiceScopeFactory scopeFactory, ILogger<InsertService2> logger)
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
                            if (!await context.Dsreplays.AnyAsync(a => a.Hash == replay.Hash))
                            {
                                foreach (var pl in replay.Dsplayers)
                                {
                                    var player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Name == pl.Name);
                                    if (player == null)
                                    {
                                        player = new DsPlayerName()
                                        {
                                            Name = pl.Name,
                                        };
                                        context.DsPlayerNames.Add(player);
                                    }
                                    pl.PlayerName = player;
                                }
                                context.Dsreplays.Add(replay);
                                await context.SaveChangesAsync();
                            }
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
                    OnReplaysInserted(new EventArgs());
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
