using EFCore.BulkExtensions;
using ElectronNET.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sc2dsstats.decode.Models;
using sc2dsstats.decode.Service;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        public int DBCount = 0;
        private int Limit = 0;
        public bool isScanning = false;
        public static bool isFirstRun = true;
        private DSoptions _options;

        public Status(ILogger<Status> logger, DSReplayContext context, DSoptions options)
        {
            _logger = logger;
            _context = context;
            _options = options;

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
            await Task.Run(() => { 
                lock (DSdata.DesktopStatus)
                {
                    Status.NewReplays = new Dictionary<string, string>();
                    Dictionary<string, string> fileHashes = new Dictionary<string, string>();
                    using (var md5 = MD5.Create())
                    {
                        foreach (var directory in DSdata.Config.Replays)
                        {
                            string dirHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(directory))).Replace("-", "").ToLowerInvariant();
                            foreach (var file in Directory.GetFiles(directory, "Direct Strike*.SC2Replay", SearchOption.AllDirectories))
                            {

                                string fileHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(file)))).Replace("-", "").ToLowerInvariant();

                                fileHashes[dirHash + fileHash] = file;
                            }
                        }
                    }
                    DSdata.DesktopStatus.FoldersReplays = fileHashes.Count;

                    HashSet<string> dbHashes = new HashSet<string>();
                    dbHashes = _context.DSReplays.Select(s => s.REPLAY).ToHashSet();

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
            bool result = await Task.Run(() => {
                try
                {
                    return DSrest.AutoUpload(_context);
                } catch
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

        public void DecodeReplays()
        {
            args.UploadStatus = UploadStatus.UploadDone;
            Decode.Doit(NewReplays.Values.ToList(), DSdata.Config.Cores);
            OnDataLoaded(args);
        }

        public async Task PopulateDb(bool fin = false)
        {
            if (DBCount == 0)
                Limit = 0;

            Interlocked.Increment(ref Limit);
            if (Limit > 2 && fin == false)
                return;

            lock (lockobject)
            {
                while (Decode.s2dec.Replays.Any())
                {
                    dsreplay rep;
                    Decode.s2dec.Replays.TryTake(out rep);
                    if (rep != null)
                    {
                        try
                        {
                            DBService.SaveReplay(_context, Map.Rep(rep));
                            Interlocked.Increment(ref DBCount);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            return;
                        }
                    }
                }
                if (fin == true)
                {
                    DSdata.Status.Count = _context.DSReplays.Count();
                }
            }
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
    }



    public class StatusChangedEventArgs : EventArgs
    {
        public int Count { get; set; } = 0;
        public int Decoded { get; set; } = 0;
        public int NewReplays { get; set; } = 0;
        public int TotalReplays { get; set; } = 0;
        public bool isReplayFolderScanned { get; set; } = false;
        public bool isReplaysDecoded { get; set; } = false;
        public UploadStatus UploadStatus { get; set; } = UploadStatus.UploadDone;
    }


}
