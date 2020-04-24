using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sc2dsstats.decode;
using sc2dsstats.decode.Service;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using sc2dsstats.shared.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.desktop.Service
{
    public class Status
    {

        public event EventHandler StatusChanged;
        private StatusChangedEventArgs args = new StatusChangedEventArgs();
        private readonly ILogger _logger;
        public static Dictionary<string, string> ReplayFolder { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> NewReplays { get; set; } = new Dictionary<string, string>();
        private static object lockobject = new object();
        private int Limit = 0;
        public bool isScanning = false;
        public static bool isFirstRun = true;
        private DSoptions _options;
        private OnTheFlyScan _onthefly;
        Regex reg = new Regex(@"\\Direct Strike|\\DST(\(\d+\))?");
        private readonly IServiceScopeFactory _scope;
        private DBService _db;
        private DecodeReplays _decode;

        public Status(ILogger<Status> logger, DBService db, DSoptions options, OnTheFlyScan onthefly, DecodeReplays decode, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _options = options;
            _onthefly = onthefly;
            _scope = scopeFactory;
            _db = db;
            _decode = decode;

            _logger.LogInformation("Start.");
            foreach (var ent in DSdata.Config.Replays)
            {
                string reppath = ent;
                if (reppath.EndsWith("/") || reppath.EndsWith("\\"))
                    reppath.Remove(reppath.Length - 1);
                var plainTextBytes = Encoding.UTF8.GetBytes(reppath);
                MD5 md5 = new MD5CryptoServiceProvider();
                string reppath_md5 = BitConverter.ToString(md5.ComputeHash(plainTextBytes));
                ReplayFolder[reppath] = reppath_md5;
            }
            //ScanReplayFolders();

            //if (isFirstRun && File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_web\\data.json")) {
            //    BulkInsertOldData();
            //}

            if (DSdata.Config.OnTheFlyScan)
                _onthefly.Start(this);
        }

        protected virtual void OnDataLoaded(StatusChangedEventArgs e)
        {
            EventHandler handler = StatusChanged;
            handler?.Invoke(this, e);
        }

        public async Task ScanReplayFolders()
        {
            if (isScanning || _options.Decoding)
                return;
            isScanning = true;
            int totalReps = 0;
            await Task.Run(() =>
            {
                lock (DSdata.DesktopStatus)
                {
                    Status.NewReplays = new Dictionary<string, string>();
                    Dictionary<string, string> fileHashes = new Dictionary<string, string>();
                    using (var md5 = MD5.Create())
                    {
                        foreach (var directory in DSdata.Config.Replays)
                        {
                            string dirHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(directory))).Replace("-", "").ToLowerInvariant();
                            foreach (var file in Directory.GetFiles(directory, "*.SC2Replay", SearchOption.AllDirectories).Where(path => reg.IsMatch(path)))
                            {

                                string fileHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(file)))).Replace("-", "").ToLowerInvariant();

                                fileHashes[dirHash + fileHash] = file;
                            }
                        }
                    }
                    DSdata.DesktopStatus.FoldersReplays = fileHashes.Count;

                    HashSet<string> dbHashes = new HashSet<string>();
                    dbHashes = _db.GetReplayHashes();
                    HashSet<string> fileKeys = fileHashes.Keys.ToHashSet();
                    fileKeys.ExceptWith(dbHashes);

                    foreach (var ent in fileKeys)
                        Status.NewReplays[ent] = fileHashes[ent];

                    DSdata.DesktopStatus.NewReplays = fileKeys.Count;
                    DSdata.DesktopStatus.DatabaseReplays = dbHashes.Count;
                }
            });
            lock (args)
            {
                args.Count = NewReplays.Count;
                args.TotalReplays = totalReps;
                args.NewReplays = NewReplays.Count();
                args.isReplayFolderScanned = true;
            }
            OnDataLoaded(args);
            isScanning = false;
        }





        public async Task UploadReplays()
        {
            args.UploadStatus = UploadStatus.Uploading;
            OnDataLoaded(args);
            bool result = await Task.Run(() =>
            {
                try
                {
                    lock (DSdata.DesktopStatus)
                    {
                        return DSrest.AutoUpload(_scope, _logger);
                    }
                }
                catch
                {
                    return false;
                }
            });
            if (result)
                args.UploadStatus = UploadStatus.UploadSuccess;
            else
                args.UploadStatus = UploadStatus.UploadFailed;
            OnDataLoaded(args);
        }

        public void DecodeReplays(List<string> Replays = null)
        {
            if (Replays != null && Replays.Any())
            {
                if (_options.Decoding)
                    return;
                _options.OnTheFlyScan = true;
                NewReplays = Replays.ToDictionary(x => x, x => x);
            }
            else if (Replays != null)
                return;
            else
            {
                if (_onthefly.Running)
                {
                    try
                    {
                        _onthefly.Stop();
                    } catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
                _options.OnTheFlyScan = false;
            }
            _options.Decoding = true;
            args.UploadStatus = UploadStatus.UploadDone;
            args.inDB = 0;
            args.Decoded = 0;
            args.NewReplays = NewReplays.Count;
            args.Info = "Engine start.";
            _decode.ScanStateChanged += DecodeUpdate;
            _decode.Doit(NewReplays.Values.ToList(), _db, DSdata.Config.Cores);
            OnDataLoaded(args);

            if (DSdata.DesktopStatus.DatabaseReplays == 0)
            {
                Task tsscan = Task.Factory.StartNew(() =>
                {
                    while (_options.Decoding && _options.Replay == null)
                    {
                        _options.Replay = _db.GetLatestReplay();
                        Task.Delay(1000).GetAwaiter().GetResult();
                    }
                });
            }
        }

        public void StopDecode()
        {
            _decode.StopIt();
            lock (args)
            {
                args.Info = "Stopping Threads..";
            }
            OnDataLoaded(args);
        }

        public void DecodeFinished()
        {
            _options.Decoding = false;
            if (DSdata.Config.Uploadcredential)
                UploadReplays();
            ScanReplayFolders();
            _options.Replay = _db.GetLatestReplay();
            OnDataLoaded(args);
            Reset();
        }

        public static void SaveConfig()
        {
            lock (lockobject)
            {
                Dictionary<string, UserConfig> temp = new Dictionary<string, UserConfig>();
                temp.Add("Config", DSdata.Config);
                var json = JsonSerializer.Serialize(temp, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(Program.myConfig, json);
            }
        }

        public void InitDB(int count = 0)
        {
            int i = 0;

            using (var md5 = MD5.Create())
            {

                foreach (string line in File.ReadAllLines(@"C:\Users\pax77\AppData\Local\sc2dsstats_web\data.json"))
                {
                    dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                    rep.Init();
                    rep.GenHash();

                    string reppath = ReplayFolder.Where(x => x.Value == rep.REPLAY.Substring(0, 47)).FirstOrDefault().Key;
                    reppath += "/" + rep.REPLAY.Substring(48);
                    reppath += ".SC2Replay";
                    string reppathhash = rep.REPLAY;
                    if (File.Exists(reppath))
                    {
                        string dirHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetDirectoryName(reppath)))).Replace("-", "").ToLowerInvariant();
                        string fileHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(reppath)))).Replace("-", "").ToLowerInvariant();
                        reppathhash = dirHash + fileHash;
                    }

                    //InsertdsDesktopReplay(_context, rep, reppathhash, reppath);

                    i++;
                    if (i % 100 == 0)
                    {
                        Console.WriteLine(i);
                        if (count > 0 && i > count)
                            break;
                    }
                }

            }

        }

        public void Reset()
        {
            DataService.Reset();
            BuildService.Reset();
        }

        void DecodeUpdate(object sender, EventArgs e)
        {
            ScanState scanState = e as ScanState;

            float wr = MathF.Round((float)scanState.Done * 100 / (float)scanState.Total, 2);
            lock (args)
            {
                args.Decoded = scanState.Done;
                args.Done = wr;
                args.Info = $"{scanState.Done}/{DSdata.DesktopStatus.NewReplays} ({wr}%), ETA: {scanState.ETA.ToString(@"hh\:mm\:ss")} - Running on {scanState.Threads + 1} Threads.";
                args.inDB = scanState.DbDone;

                if (scanState.End != DateTime.MinValue)
                {
                    if (scanState.Done == scanState.Total)
                        wr = 100;
                    args.Info = $"{scanState.Done}/{scanState.Total} ({wr}%) - Elapsed Time: {(scanState.End - scanState.Start).ToString(@"hh\:mm\:ss")}";
                    if (_options.Decoding)
                        DecodeFinished();
                }
            }
            OnDataLoaded(args);
        }
    }



    public class StatusChangedEventArgs : EventArgs
    {
        public int Count { get; set; } = 0;
        public int Decoded { get; set; } = 0;
        public int inDB = 0;
        public int NewReplays { get; set; } = 0;
        public float Done { get; set; } = 0;
        public int TotalReplays { get; set; } = 0;
        public bool isReplayFolderScanned { get; set; } = false;
        public bool isReplaysDecoded { get; set; } = false;
        public string Info { get; set; } = "";
        public UploadStatus UploadStatus { get; set; } = UploadStatus.UploadDone;
    }


}
