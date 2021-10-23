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
using System.Runtime.CompilerServices;
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
        public HashSet<string> NewReplays { get; set; } = new HashSet<string>();
        private static object lockobject = new object();
        public bool isScanning = false;
        public static bool isFirstRun = true;
        private DSoptions _options;
        private OnTheFlyScan _onthefly;
        Regex reg = new Regex(@"\\Direct Strike|\\DST(\(\d+\))?");
        private DBService _db;
        private DecodeReplays _decode;
        private DSrest _rest;

        public Status(ILogger<Status> logger, DBService db, DSoptions options, OnTheFlyScan onthefly, DecodeReplays decode, DSrest rest)
        {
            _logger = logger;
            _options = options;
            _onthefly = onthefly;
            _db = db;
            _decode = decode;
            _rest = rest;

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
            HashSet<string> DbReplays = null;
            await Task.Run(() => {
                DbReplays = new HashSet<string>(_db.GetDbReplays());
            });
            await Task.Run(() => {
                DateTime t = DateTime.UtcNow;
                NewReplays = new HashSet<string>(Scan.ScanReplayFolders(DbReplays));
                _logger.LogInformation($"RepFolders scanned in: {(DateTime.UtcNow - t).TotalSeconds}");
            });

            lock (args)
            {
                DSdata.DesktopStatus.NewReplays = NewReplays.Count;
                DSdata.DesktopStatus.DatabaseReplays = DbReplays.Count;
                args.Count = NewReplays.Count;
                args.NewReplays = NewReplays.Count;
                args.TotalReplays = DbReplays.Count + NewReplays.Count;
                args.isReplayFolderScanned = true;
            }
            OnDataLoaded(args);
        }



        public async Task UploadReplays()
        {
            args.UploadStatus = UploadStatus.Uploading;
            OnDataLoaded(args);

            bool success = false;
            try {
                success = await _rest.AutoUpload();
            } catch {}

            if (success)
                args.UploadStatus = UploadStatus.UploadSuccess;
            else
                args.UploadStatus = UploadStatus.UploadFailed;
            OnDataLoaded(args);
        }

        public int DecodeReplays(string otfreplay = "")
        {
            if (_options.Decoding)
                return -1;
            _options.Decoding = true;
            ScanReplayFolders().GetAwaiter();

            if (!String.IsNullOrEmpty(otfreplay))
            {
                while (!NewReplays.Contains(otfreplay))
                {
                    _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.fff")} Waiting for otfreplay: {otfreplay}");
                    Task.Delay(250).GetAwaiter().GetResult();
                    ScanReplayFolders().GetAwaiter();
                }
            }

            args.UploadStatus = UploadStatus.UploadDone;
            args.inDB = 0;
            args.Decoded = 0;
            args.NewReplays = NewReplays.Count;
            args.Info = "Engine start.";
            _decode.ScanStateChanged += DecodeUpdate;
            _logger.LogInformation("Decoding start.");
            _decode.Doit(NewReplays, DSdata.Config.Cores);
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
            return NewReplays.Count;
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
            Reset();
            if (DSdata.Config.OnTheFlyScan)
                _onthefly.Start(this);
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
