using Blazored.Toast.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using sc2dsstats.db.Stats;
using sc2dsstats.decode;
using sc2dsstats.lib.Db;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sc2dsstats.app.Services
{
    public class ReplayService
    {
        public List<string> DbReplayPaths = new List<string>();
        private CancellationTokenSource source;
        public AppConfig AppConfig;

        public List<string> NewReplays = new List<string>();
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<ReplayService> logger;
        public bool isFirstRun = true;
        private Regex rx_ds = new Regex(@"(Direct Strike.*)\.SC2Replay$|(DST.*)\.SC2Replay$", RegexOptions.Singleline);
        public event EventHandler<ScanEventArgs> ReplayFoldersScanned;
        public event EventHandler<UploadEventArgs> ReplaysUploaded;
        public event EventHandler<CollectEventArgs> CollectReplayStats;
        public DecodeService decodeService;
        private object lockobject = new object();
        public WatchService watchService;
        public string latestReplay = String.Empty;
        public DsPlayerStats playerStats;
        public ElectronService electronService = new ElectronService();
        private InsertService InsertService;
        public bool isWatching { get; private set; }

        public ReplayService(IServiceScopeFactory scopeFactory, ILogger<ReplayService> logger, InsertService insertService)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
            InsertService = insertService;
            if (!File.Exists(Program.myConfig))
            {
                isFirstRun = true;
                AppConfig = FirstRunService.GetInitialConfig(logger);
                SaveConfig();
            }
            else
            {
                AppConfig = new AppConfig() { Config = new UserConfig() };
                using (var scope = scopeFactory.CreateScope())
                {
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("Config");
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    config.Bind(AppConfig.Config);
                }
                isFirstRun = false;
            }
            decodeService = new DecodeService();
            decodeService.DecodeStateChanged += DecodeService_DecodeStateChanged;
            _ = ScanReplayFolders();
            _ = electronService.CheckForUpdate();
            if (AppConfig.Config.OnTheFlyScan)
            {
                StartWatching();
            }
        }

        public async Task LoadPlayerStats()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                playerStats = await dataService.GetPlayerStats(AppConfig.Config.PlayersNames);
            }
        }

        public void TestNewReplay()
        {

        }

        private void WatchService_NewFileDetected(object sender, FileSystemEventArgs e)
        {
            DecodeReplays();
        }

        public void StartWatching()
        {
            isWatching = true;
            if (watchService != null)
            {
                watchService.Stop();
                watchService.NewFileDetected -= WatchService_NewFileDetected;
            }
            else
            {
                watchService = new WatchService();
            }
            watchService.Watch(AppConfig.Config.ReplayPaths);
            watchService.NewFileDetected += WatchService_NewFileDetected;
        }

        public void StopWatching()
        {
            isWatching = false;
            if (watchService != null)
            {
                watchService.Stop();
                watchService.NewFileDetected -= WatchService_NewFileDetected;
            }
        }

        public void SaveConfig()
        {
            lock (lockobject)
            {
                var json = JsonSerializer.Serialize(AppConfig, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(Program.myConfig, json);
            }
        }

        public async Task ScanReplayFolders(bool force = false)
        {
            if (DbReplayPaths.Count == 0 || force)
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    if (String.IsNullOrEmpty(latestReplay))
                    {
                        var latestRep = context.Dsreplays.AsNoTracking().OrderByDescending(o => o.Gametime).FirstOrDefault();
                        if (latestRep != null)
                            latestReplay = latestRep.Hash;
                    }
                    DbReplayPaths = await context.Dsreplays.Select(s => s.Replaypath).Distinct().ToListAsync();
                }
            }

            List<string> HdReplayPaths = new List<string>();
            foreach (var replayPath in AppConfig.Config.ReplayPaths)
            {
                HdReplayPaths.AddRange(Directory.GetFiles(replayPath, "*", SearchOption.AllDirectories).Where(x => rx_ds.IsMatch(x)));
            }
            NewReplays = HdReplayPaths.Except(DbReplayPaths).ToList();
            ScanEventArgs scanArgs = new ScanEventArgs()
            {
                Count = NewReplays.Count
            };
            OnReplayFoldersScanned(scanArgs);
        }

        protected virtual void OnReplayFoldersScanned(ScanEventArgs e)
        {
            EventHandler<ScanEventArgs> handler = ReplayFoldersScanned;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnReplaysUploading(UploadEventArgs e)
        {
            EventHandler<UploadEventArgs> handler = ReplaysUploaded;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnCollectingStats(CollectEventArgs e)
        {
            EventHandler<CollectEventArgs> handler = CollectReplayStats;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public async void ImportOldDb()
        {
            lock (lockobject)
            {
                if (decodeService.isRunning)
                {
                    return;
                }
                decodeService.isRunning = true;
            }
            if (File.Exists(Path.Combine(Program.workdir, "data_v3_0.db")))
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    var oldcontext = scope.ServiceProvider.GetRequiredService<DSReplayContext>();

                    int skip = 0;
                    int take = 100;
                    DateTime start = DateTime.UtcNow;

                    decodeService.OnDecodeStateChanged(new DecodeStateEvent()
                    {
                        Threads = 1,
                        Done = 0,
                        Failed = 0,
                        Running = true,
                        StartTime = start
                    });

                    var oldReplays = await oldcontext.DSReplays
                    .Include(i => i.Middle)
                    .Include(i => i.DSPlayer)
                        .ThenInclude(j => j.Breakpoints)
                    .AsNoTracking()
                    .OrderBy(o => o.GAMETIME)
                    .AsSplitQuery()
                    .Take(take)
                    .ToListAsync();

                    source = new CancellationTokenSource();

                    while (oldReplays.Any() && !source.IsCancellationRequested)
                    {


                        decodeService.OnDecodeStateChanged(new DecodeStateEvent()
                        {
                            Threads = 1,
                            Done = skip + take,
                            Failed = 0,
                            Running = true,
                            StartTime = start
                        });

                        var json = JsonSerializer.Serialize(oldReplays);
                        var newReplays = JsonSerializer.Deserialize<List<DsReplayDto>>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                        EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
                        InsertService.InsertReplays(newReplays, AppConfig.Config.PlayersNames, ewh);
                        await Task.Run(() => { ewh.WaitOne(); });
                        skip += take;
                        oldReplays = await oldcontext.DSReplays
                                            .Include(i => i.Middle)
                                            .Include(i => i.DSPlayer)
                                                .ThenInclude(j => j.Breakpoints)
                                            .AsNoTracking()
                                            .OrderBy(o => o.GAMETIME)
                                            .AsSplitQuery()
                                            .Skip(skip)
                                            .Take(take)
                                            .ToListAsync();
                    }
                    decodeService.isRunning = false;
                    isFirstRun = false;
                    UploadService_ReplaysInserted();
                }
            }
        }

        public async void DecodeReplays(IToastService toastService = null)
        {
            logger.LogInformation("DecodeReplays");

            if (toastService != null)
                toastService.ShowWarning("Start decoding ...");

            if (String.IsNullOrEmpty(ElectronService.AppPath))
            {
                await ElectronService.GetPath();
            }

            if (!NewReplays.Any())
            {
                await ScanReplayFolders();
            }

            if (NewReplays.Any() && !decodeService.isRunning)
            {

                source = new CancellationTokenSource();
                _ = decodeService.DecodeReplays(ElectronService.AppPath, NewReplays, AppConfig.Config.CPUCores, source.Token);
            }
            else if (toastService != null)
            {
                if (decodeService.isRunning)
                {
                    toastService.ShowError("The decoding process is already running");
                }
                else if (!NewReplays.Any())
                {
                    toastService.ShowError("No new replays to decode available");
                }
            }
        }

        public void DecodeCancel()
        {
            source.Cancel();
        }

        private void DecodeService_DecodeStateChanged(object sender, DecodeStateEvent e)
        {
            logger.LogInformation($"Decoding Replays: {e.Done} on {e.Threads} threads.");

            if (e.Running == false)
                InsertReplays(true);
            else
                InsertReplays();
        }

        private void InsertReplays(bool isDone = false)
        {
            List<Dsreplay> replays = new List<Dsreplay>();
            Dsreplay replay;
            while (decodeService.Replays.TryTake(out replay))
                replays.Add(replay);

            if (replays.Any() || isDone)
            {
                if (isDone)
                {
                    logger.LogInformation($"Decoding Replays done.");
                    EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
                    InsertService.InsertReplays(replays.Select(s => s.GetDto()).ToList(), AppConfig.Config.PlayersNames, ewh);
                    ewh.WaitOne();
                    UploadService_ReplaysInserted();
                }
                else
                {
                    InsertService.InsertReplays(replays.Select(s => s.GetDto()).ToList(), AppConfig.Config.PlayersNames);
                }
            }
        }

        public async Task UploadReplays()
        {
            if (AppConfig.Config.Uploadcredential)
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dataServcie = scope.ServiceProvider.GetRequiredService<IDataService>();
                    OnReplaysUploading(new UploadEventArgs() { uploadStatus = UploadStatus.Uploading });
                    bool success = await dataServcie.UploadData();
                    if (success)
                    {
                        OnReplaysUploading(new UploadEventArgs() { uploadStatus = UploadStatus.Success });
                    }
                    else
                    {
                        OnReplaysUploading(new UploadEventArgs() { uploadStatus = UploadStatus.Failed });
                    }
                }
            }
        }

        private async void UploadService_ReplaysInserted()
        {
            logger.LogInformation("UploadService_ReplaysInserted");
            await UploadReplays();
            DbReplayPaths.Clear();
            await UpdateStats();
            OnCollectingStats(new CollectEventArgs());
            _ = ScanReplayFolders();
        }

        private async Task UpdateStats()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                await UpdatePlayers(context);

                var stats = await StatsService.GetStats(context, false);
                memoryCache.Set("cmdrstats", stats);
                var plstats = await StatsService.GetStats(context, true);
                memoryCache.Set("cmdrstatsplayer", stats);
                var latestRep = context.Dsreplays.OrderByDescending(o => o.Gametime).FirstOrDefault();
                if (latestRep != null)
                    latestReplay = latestRep.Hash;

            }
        }

        private async Task UpdatePlayers(sc2dsstatsContext context)
        {
            var playernames = await context.DsPlayerNames.ToListAsync();
            var player = playernames.FirstOrDefault(f => f.AppId == AppConfig.Config.AppId);
            if (player == null)
            {
                player = new DsPlayerName()
                {
                    AppId = AppConfig.Config.AppId,
                    DbId = AppConfig.Config.DbId,
                    Name = AppConfig.Config.PlayersNames.FirstOrDefault()
                };
                context.DsPlayerNames.Add(player);
                await context.SaveChangesAsync();
            }
            var dsplayers = await context.Dsplayers.Where(x => x.PlayerName == null).ToListAsync();
            foreach (var dsplayer in dsplayers)
            {
                if (dsplayer.isPlayer)
                {
                    dsplayer.PlayerName = player;
                }
                else
                {
                    var playername = playernames.FirstOrDefault(f => f.Name == dsplayer.Name);
                    if (playername == null)
                    {
                        playername = new DsPlayerName()
                        {
                            Name = dsplayer.Name
                        };
                        context.DsPlayerNames.Add(playername);
                        playernames.Add(playername);
                    }
                    dsplayer.PlayerName = playername;
                }

            }
            await context.SaveChangesAsync();
        }

        //private void CollectStats()
        //{
        //    logger.LogInformation("CollectStats");
        //    lock (lockobject)
        //    {
        //        using (var scope = scopeFactory.CreateScope())
        //        {
        //            var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
        //            var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
        //            var latestRep = context.Dsreplays.OrderByDescending(o => o.Gametime).FirstOrDefault();
        //            if (latestRep != null)
        //                latestReplay = latestRep.Hash;
        //            OnCollectingStats(new CollectEventArgs()
        //            {
        //                Collecting = true
        //            });
        //            UploadService.CollectTimeResults2(context, logger);
        //        }
        //    }
        //    OnCollectingStats(new CollectEventArgs()
        //    {
        //        Collecting = false
        //    });

        //}
    }

    public class ScanEventArgs : EventArgs
    {
        public int Count { get; set; }
    }

    public class CollectEventArgs : EventArgs
    {
        public bool Collecting { get; set; }
    }

    public class UploadEventArgs : EventArgs
    {
        public UploadStatus uploadStatus { get; set; }
    }

    public enum UploadStatus
    {
        None,
        Uploading,
        Success,
        Failed
    }
}
