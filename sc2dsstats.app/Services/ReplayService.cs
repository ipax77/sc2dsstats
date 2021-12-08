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
    public class ReplayService : IDisposable
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
        private object lockobject = new object();
        public WatchService watchService;
        public string latestReplay = String.Empty;
        public DsPlayerStats playerStats;
        public ElectronService electronService = new ElectronService();
        private ProducerService producerService;
        public bool isWatching { get; private set; }

        public ReplayService(IServiceScopeFactory scopeFactory, ILogger<ReplayService> logger, ProducerService producerService)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
            this.producerService = producerService;
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
            _ = ScanReplayFolders();
            _ = electronService.CheckForUpdate(5000);
            if (AppConfig.Config.OnTheFlyScan)
            {
                StartWatching();
            }
            producerService.ReplayChannelChanged += ProducerService_ReplayChannelChanged;
        }

        private void ProducerService_ReplayChannelChanged(object sender, ReplayChannelEventArgs e)
        {
            if (e.Done)
            {
                _ = InsertJobDone();
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
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    foreach (var playerName in AppConfig.Config.PlayersNames.Where(x => !String.IsNullOrEmpty(x)))
                    {
                        var player = context.DsPlayerNames.FirstOrDefault(f => f.Name == playerName);
                        if (player == null)
                        {
                            player = new DsPlayerName()
                            {
                                AppId = AppConfig.Config.AppId,
                                Name = playerName
                            };
                            context.DsPlayerNames.Add(player);
                            context.SaveChanges();
                        }
                        else if (player.AppId == Guid.Empty)
                        {
                            player.AppId = AppConfig.Config.AppId;
                            context.SaveChanges();
                        }
                    }
                }
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

        public void ImportOldDb()
        {
            lock (lockobject)
            {
                if (producerService.Producing)
                {
                    return;
                }
            }
            if (File.Exists(Path.Combine(Program.workdir, "data_v3_0.db")))
            {
                Task.Run(async () =>
                {
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                        var oldcontext = scope.ServiceProvider.GetRequiredService<DSReplayContext>();
                        source = new CancellationTokenSource();
                        await producerService.ProduceFromOldDb(context, oldcontext, AppConfig.Config.PlayersNames, source.Token);
                        isFirstRun = false;
                    }
                });
            }
        }

        public void DecodeReplays(IToastService toastService = null)
        {
            logger.LogInformation("DecodeReplays");

            if (toastService != null)
                toastService.ShowWarning("Start decoding ...");

            Task.Run(async () =>
            {
                if (String.IsNullOrEmpty(ElectronService.AppPath))
                {
                    await ElectronService.GetPath();
                }

                if (!NewReplays.Any())
                {
                    await ScanReplayFolders();
                }

                if (NewReplays.Any() && !producerService.Producing)
                {

                    source = new CancellationTokenSource();
                    producerService.Produce(ElectronService.AppPath, AppConfig.Config.PlayersNames, NewReplays, source.Token, AppConfig.Config.CPUCores);
                }
                else if (toastService != null)
                {
                    if (producerService.Producing)
                    {
                        toastService.ShowError("The decoding process is already running");
                    }
                    else if (!NewReplays.Any())
                    {
                        toastService.ShowError("No new replays to decode available");
                    }
                }
            });
        }

        public void DecodeCancel()
        {
            source?.Cancel();
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

        private async Task InsertJobDone()
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

                var stats = await StatsService.GetStats(context, false);
                memoryCache.Set("cmdrstats", stats);
                var plstats = await StatsService.GetStats(context, true);
                memoryCache.Set("cmdrstatsplayer", stats);
                var latestRep = context.Dsreplays.OrderByDescending(o => o.Gametime).FirstOrDefault();
                if (latestRep != null)
                    latestReplay = latestRep.Hash;

            }
        }

        public void Dispose()
        {
            logger.LogInformation("Disposing ReplayServcie");
            source?.Cancel();
        }
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
