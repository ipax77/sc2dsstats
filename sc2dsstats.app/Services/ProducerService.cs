using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using sc2dsstats.decode;
using sc2dsstats.lib.Db;
using System.Collections.Concurrent;
using System.Text.Json;

namespace sc2dsstats.app.Services
{
    public class ProducerService
    {
        private readonly ILogger<ProducerService> logger;
        private readonly IInsertService insertService;

        public ProducerService(ILogger<ProducerService> logger, IInsertService insertService)
        {
            this.logger = logger;
            this.insertService = insertService;
        }

        private string AppPath = String.Empty;
        private List<string> PlayerNames;
        private int Threads = 2;
        private int producerCount = 0;
        private int decodeCount = 0;
        private int insertCount = 0;
        private DateTime StartTime = DateTime.MinValue;

        private object lockobject = new object();
        public bool Producing { get; private set; } = false;

        public ConcurrentBag<string> FailedReplays = new ConcurrentBag<string>();
        private CancellationTokenSource notifySource;

        public void Produce(string appPath, List<string> playerNames, List<string> replayPaths, CancellationToken token, int threads = 4)
        {
            if (Producing)
                return;
            Producing = true;
            AppPath = appPath;
            PlayerNames = playerNames;
            Threads = threads;

            producerCount = 0;
            decodeCount = 0;
            FailedReplays = new ConcurrentBag<string>();

            StartTime = DateTime.UtcNow;

            insertService.WriteStart();
            _ = ProduceReplays(replayPaths, token);
            notifySource = new CancellationTokenSource();
            _ = Notify(notifySource.Token);
            insertService.ReplaysInserted += InsertService_ReplaysInserted;
        }

        public async Task ProduceFromOldDb(sc2dsstatsContext context, DSReplayContext oldcontext, List<string> playerNames, CancellationToken token)
        {
            if (Producing)
                return;
            Producing = true;
            PlayerNames = playerNames;
            int skip = 0;
            int take = 250;
            Threads = 1;
            producerCount = 1;
            decodeCount = 0;
            FailedReplays = new ConcurrentBag<string>();
            StartTime = DateTime.UtcNow;

            insertService.WriteStart();
            notifySource = new CancellationTokenSource();
            _ = Notify(notifySource.Token);
            insertService.ReplaysInserted += InsertService_ReplaysInserted;

            try
            {
                var oldReplays = await oldcontext.DSReplays
                    .Include(i => i.Middle)
                    .Include(i => i.DSPlayer)
                        .ThenInclude(j => j.Breakpoints)
                    .AsNoTracking()
                    .OrderByDescending(o => o.GAMETIME)
                    .AsSplitQuery()
                    .Take(take)
                    .ToListAsync();
                while (oldReplays.Any() && !token.IsCancellationRequested)
                {
                    var json = JsonSerializer.Serialize(oldReplays);
                    var newReplays = JsonSerializer.Deserialize<List<DsReplayDto>>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    newReplays.ForEach(f => InsertReplay(f, PlayerNames));

                    await Task.Delay(5000); // TODO

                    skip += take;
                    decodeCount += take;
                    oldReplays = await oldcontext.DSReplays
                                        .Include(i => i.Middle)
                                        .Include(i => i.DSPlayer)
                                            .ThenInclude(j => j.Breakpoints)
                                        .AsNoTracking()
                                        .OrderByDescending(o => o.GAMETIME)
                                        .AsSplitQuery()
                                        .Skip(skip)
                                        .Take(take)
                                        .ToListAsync();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError($"failed importing old db: {ex.Message}");
            }
            finally
            {
                PrducerFinished();
            }
        }

        private void InsertService_ReplaysInserted(object sender, InsertEventArgs e)
        {
            if (e.Done)
            {
                notifySource.Cancel();
                Producing = false;
                insertService.ReplaysInserted -= InsertService_ReplaysInserted;
            }
            else
            {
                insertCount = e.insertCount;
            }
        }

        public event EventHandler<ReplayChannelEventArgs> ReplayChannelChanged;
        protected virtual void OnReplayChannelChanged(ReplayChannelEventArgs e)
        {
            EventHandler<ReplayChannelEventArgs> handler = ReplayChannelChanged;
            handler?.Invoke(this, e);
        }


        private async Task Notify(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var channelEvent = new ReplayChannelEventArgs()
                {
                    Threads = Threads,
                    producerCount = producerCount,
                    decodeCount = decodeCount,
                    insertCount = insertCount,
                    StartTime = StartTime,
                    failedCount = FailedReplays.Count
                };
                OnReplayChannelChanged(channelEvent);
                await Task.Delay(1000);
            }
            OnReplayChannelChanged(new ReplayChannelEventArgs()
            {
                Threads = Threads,
                producerCount = producerCount,
                decodeCount = decodeCount,
                insertCount = insertCount,
                Done = true,
                StartTime = StartTime,
                failedCount = FailedReplays.Count
            });
            logger.LogInformation($"notify canceled");
        }

        private async Task ProduceReplays(List<string> replayPaths, CancellationToken token)
        {
            try
            {
                ParallelOptions po = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Threads,
                    CancellationToken = token
                };
                await Parallel.ForEachAsync(replayPaths, po, async (data, token) =>
                {
                    Interlocked.Increment(ref producerCount);
                    logger.LogInformation($"working on {Path.GetFileName(data)}");
                    DsReplayDto replayDto = null;
                    await Task.Run(() => { replayDto = DsDecodeService.DecodeReplay(AppPath, data, token); }, token);
                    if (replayDto != null)
                    {
                        InsertReplay(replayDto, PlayerNames);
                        Interlocked.Increment(ref decodeCount);
                    }
                    else
                    {
                        FailedReplays.Add(data);
                    }
                    Interlocked.Decrement(ref producerCount);
                    po.CancellationToken.ThrowIfCancellationRequested();
                });
            }
            catch (OperationCanceledException)
            {
                insertService.Cancel();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                PrducerFinished();
            }
            logger.LogInformation($"producer job finished.");
        }

        private void InsertReplay(DsReplayDto replay, List<string> playerNames)
        {
            var dbReplay = new Dsreplay(replay);
            dbReplay.Dsplayers.Where(x => playerNames.Contains(x.Name)).ToList().ForEach(x => x.isPlayer = true);
            ReplayFilter.SetDefaultFilter(new List<Dsreplay>() { dbReplay });
            DbService.SetMid(dbReplay);
            insertService.AddReplay(dbReplay);
        }

        private void PrducerFinished()
        {
            insertService.WriteFinished();
        }
    }
}
public class ReplayChannelEventArgs : EventArgs
{
    public DateTime StartTime { get; set; }
    public int Threads { get; set; }
    public int producerCount { get; set; }
    public int decodeCount { get; set; }
    public int insertCount { get; set; }
    public int failedCount { get; set; }
    public bool Done { get; set; } = false;
}


