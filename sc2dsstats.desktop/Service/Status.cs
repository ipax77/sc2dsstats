using Microsoft.EntityFrameworkCore;
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
        private DSReplayContext _context;
        public static Dictionary<string, string> ReplayFolder { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> NewReplays { get; set; } = new Dictionary<string, string>();
        private static object lockobject = new object();
        private int Limit = 0;
        public bool isScanning = false;
        public static bool isFirstRun = true;
        private DSoptions _options;
        private OnTheFlyScan _onthefly;
        Regex reg = new Regex(@"\\Direct Strike|\\DST(\(\d+\))?");


        public Status(ILogger<Status> logger, DSReplayContext context, DSoptions options, OnTheFlyScan onthefly)
        {
            _logger = logger;
            _context = context;
            _options = options;
            _onthefly = onthefly;

            if (_options.db == null)
                _options.db = _context;

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
                    lock (_options.db)
                    {
                        dbHashes = _options.db.DSReplays.Select(s => s.REPLAY).ToHashSet();
                    }
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
                        return DSrest.AutoUpload(_context, _logger);
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
                    _onthefly.Stop();
                _options.OnTheFlyScan = false;
            }
            _options.Decoding = true;
            args.UploadStatus = UploadStatus.UploadDone;
            args.inDB = 0;
            args.Decoded = 0;
            args.NewReplays = NewReplays.Count;
            args.Info = "Engine start.";
            Decode.s2dec.ScanStateChanged += DecodeUpdate;
            Decode.Doit(NewReplays.Values.ToList(), DSdata.Config.Cores);
            //Decode.Doit(NewReplays.Values.Take(100).ToList(), DSdata.Config.Cores);
            OnDataLoaded(args);
        }

        public void StopDecode()
        {
            Decode.StopIt();
            PopulateDb(true);
            lock (args)
            {
                args.Info = "Stopping Threads..";
            }
            OnDataLoaded(args);
        }

        public async Task PopulateDb(bool fin = false)
        {
            if (args.inDB == 0)
                Limit = 0;

            if (Limit > 3 && fin == false)
                return;

            Interlocked.Increment(ref Limit);
            await Task.Run(() =>
            {
                lock (lockobject)
                {

                    while (Decode.s2dec.DSReplays.Any())
                    {
                        DSReplay rep;
                        Decode.s2dec.DSReplays.TryTake(out rep);
                        if (rep != null)
                        {
                            //DBService.SaveReplay(_options.db, rep, bulk);
                            _options.db.DSReplays.Add(rep);
                            Interlocked.Increment(ref args.inDB);
                        }
                        if (args.inDB % 10 == 0)
                        {
                            lock (_options.db)
                            {
                                try
                                {
                                    _options.db.SaveChanges();
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e.Message);
                                }
                            }
                            if (fin)
                                OnDataLoaded(args);
                        }
                    }
                    if (fin == true)
                    {
                        lock (_options.db)
                        {
                            try
                            {
                                _options.db.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e.Message);
                            }
                            DSdata.Status.Count = _options.db.DSReplays.Count();
                        }
                        _options.Decoding = false;

                        lock (_options.db)
                        {
                            _options.Replay = _options.db.DSReplays.Include(p => p.DSPlayer).ThenInclude(b => b.Breakpoints).OrderByDescending(o => o.GAMETIME).FirstOrDefault();
                        }
                        if (DSdata.Config.OnTheFlyScan && _onthefly.Running == false)
                        {
                            _options.OnTheFlyScan = true;
                            _onthefly.Start(this);
                        }

                        if (DSdata.Config.Uploadcredential)
                            UploadReplays();
                        ScanReplayFolders();
                        OnDataLoaded(args);
                        Reset();

                    }

                }
            });
            Interlocked.Decrement(ref Limit);
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
                args.Info = $"{scanState.Done}/{DSdata.DesktopStatus.NewReplays} ({wr}%), inDB: {args.inDB} - Running on {scanState.Threads + 1} Threads.";
                PopulateDb();

                if (scanState.Threads == 0 && scanState.Done >= scanState.Total)
                {
                    if (scanState.Done == scanState.Total)
                        wr = 100;
                    args.Info = $"{scanState.Done}/{scanState.Total} ({wr}%) - Elapsed Time: {(DateTime.Now - scanState.Start).ToString(@"hh\:mm\:ss")}";
                    PopulateDb(true);
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
