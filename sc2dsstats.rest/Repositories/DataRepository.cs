using Microsoft.Extensions.Logging;
using sc2dsstats.lib.Models;
using sc2dsstats.rest.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using sc2dsstats.data;
using System.Linq;
using sc2dsstats.lib.Service;
using sc2dsstats.lib.Db;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore;

namespace sc2dsstats.rest.Repositories
{
    public class DataRepository : IDataRepository
    {
        private readonly ILogger _logger;

        private static string WorkDir { get; } = "/data";
        private static string SharedDir { get; } = "/autodata";
        private static string DBDir { get; } = "/dbdata";
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private static ConcurrentBag<DSReplay> DSReplays = new ConcurrentBag<DSReplay>();
        private static object lockobject = new object();
        private readonly IServiceScope _scope;
        private static DSRestContext _context;
        private object dblock = new object();
        private ConcurrentDictionary<string, DSinfo> Infos = new ConcurrentDictionary<string, DSinfo>();


        public DataRepository(ILogger<DataRepository> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scope = scopeFactory.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<DSRestContext>();
            _context.Database.EnsureCreated();

            string data_json = WorkDir + "/data.json";
        }

        public static DSReplay GetDSReplay()
        {
            DSReplay replay = null;
            DSReplays.TryTake(out replay);
            return replay;
        }

        public string AutoInfo(DSinfo info)
        {
            DSRestPlayer player = null;
            lock (dblock)
            {
                player = _context.DSRestPlayers.FirstOrDefault(f => f.Name == info.Name);
            }

            string myreturn = "";
            if (player != null)
            {
                //DEBUG
                //player.LastRep = new DateTime(2020, 4, 21);

                DateTime LastRep = DateTime.MinValue;
                if (info.LastRep.Length == 14)
                {
                    int year = int.Parse(info.LastRep.Substring(0, 4));
                    int month = int.Parse(info.LastRep.Substring(4, 2));
                    int day = int.Parse(info.LastRep.Substring(6, 2));
                    int hour = int.Parse(info.LastRep.Substring(8, 2));
                    int min = int.Parse(info.LastRep.Substring(10, 2));
                    int sec = int.Parse(info.LastRep.Substring(12, 2));
                    LastRep = new DateTime(year, month, day, hour, min, sec);
                }
                if (player.LastRep == LastRep) myreturn = "UpToDate";
                else
                {
                    myreturn = player.LastRep.ToString("yyyyMMddHHmmss");
                }
            }
            else
            {
                player = new DSRestPlayer();
                player.Name = info.Name;
                player.Json = info.Json;
                player.Total = info.Total;
                player.Version = info.Version;
                myreturn = "0";
                _context.DSRestPlayers.Add(player);
            }
            lock (dblock)
            {
                _context.SaveChanges();
            }

            Infos[info.Name] = info;

            return myreturn;
        }

        public async Task<bool> GetDBFile(string id, string myfile)
        {
            DSRestPlayer player = null;
            lock (dblock)
            {
                player = _context.DSRestPlayers.Include(p => p.Uploads).FirstOrDefault(f => f.Name == id);
            }
            if (player == null)
                return false;

            string mypath = DBDir + "/" + id;
            string mysum = DBDir + "/sum/" + id + ".json";
            Console.WriteLine(mysum);
            if (!Directory.Exists(mypath))
            {
                try
                {
                    Directory.CreateDirectory(mypath);
                }
                catch (Exception e)
                {
                    _logger.LogError("Could not create directory " + mypath + " " + e.Message);
                    return false;
                }
            }
            DateTime t = DateTime.Now;
            if (File.Exists(myfile))
            {
                if (!File.Exists(mysum)) File.Create(mysum).Dispose();
                string myjson = "";
                return await Task.Run(() => myjson = Decompress(new FileInfo(myfile), mypath, id, _logger)).ContinueWith(task =>
                {
                    if (myjson == "") return false;
                    if (File.Exists(myjson) && new FileInfo(myjson).Length > 0)
                    {
                        using (StreamWriter sw = File.AppendText(mysum))
                        {
                            foreach (string line in File.ReadLines(myjson))
                            {
                                if (!line.StartsWith(@"{")) return false;
                                sw.WriteLine(line);
                                DSReplay replay = null;
                                try
                                {
                                    replay = JsonSerializer.Deserialize<DSReplay>(line);
                                } catch (Exception e)
                                {
                                    _logger.LogError("Could not Deserialize dsreplay " + e.Message);
                                }
                                if (replay != null)
                                {
                                    replay.Upload = DateTime.UtcNow;
                                    DSPlayer dsplayer = replay.DSPlayer.FirstOrDefault(f => f.NAME == "player");
                                    if (dsplayer != null)
                                        dsplayer.NAME = id;
                                    DSReplays.Add(replay);
                                    player.Data++;
                                }
                            }
                        }
                        DSRestUpload upload = new DSRestUpload();
                        upload.Upload = player.LastUpload;
                        upload.DSRestPlayer = player;
                        _context.DSRestUploads.Add(upload);

                        if (Infos.ContainsKey(id))
                        {
                            DateTime LastRep = DateTime.MinValue;
                            if (Infos[id].LastRep.Length == 14)
                            {
                                int year = int.Parse(Infos[id].LastRep.Substring(0, 4));
                                int month = int.Parse(Infos[id].LastRep.Substring(4, 2));
                                int day = int.Parse(Infos[id].LastRep.Substring(6, 2));
                                int hour = int.Parse(Infos[id].LastRep.Substring(8, 2));
                                int min = int.Parse(Infos[id].LastRep.Substring(10, 2));
                                int sec = int.Parse(Infos[id].LastRep.Substring(12, 2));
                                LastRep = new DateTime(year, month, day, hour, min, sec);
                            }
                            player.LastRep = LastRep;
                            player.Json = Infos[id].Json;
                            player.Total = Infos[id].Total;
                            player.Version = Infos[id].Version;
                        }

                        lock (dblock)
                        {
                            _context.SaveChanges();
                        }
                        //InsertDSReplays();
                        InsertDSReplays().GetAwaiter().GetResult();
                        return true;
                    }
                    else return false;
                });

            }
            else return false;

        }

        public async Task<bool> GetAutoFile(string id, string myfile)
        {

            DSRestPlayer player = _context.DSRestPlayers.Include(p => p.Uploads).FirstOrDefault(f => f.Name == id);
            if (player == null)
                return false;

            player.LastUpload = DateTime.UtcNow;

            string mypath = SharedDir + "/" + id;
            string mysum = SharedDir + "/sum/" + id + ".json";
            
            if (!Directory.Exists(mypath))
            {
                try
                {
                    Directory.CreateDirectory(mypath);
                }
                catch (Exception e)
                {
                    _logger.LogError("Could not create directory " + mypath + " " + e.Message);
                    return false;
                }
            }
            if (File.Exists(myfile))
            {
                if (!File.Exists(mysum)) File.Create(mysum).Dispose();
                string myjson = "";
                return await Task.Run(() => myjson = Decompress(new FileInfo(myfile), mypath, id, _logger))
                    .ContinueWith(task =>
                    {
                        if (myjson == "") return false;
                        if (File.Exists(myjson) && new FileInfo(myjson).Length > 0)
                        {
                            using (StreamWriter sw = File.AppendText(mysum))
                            {
                                foreach (string line in File.ReadLines(myjson))
                                {
                                    if (!line.StartsWith(@"{")) return false;
                                    sw.WriteLine(line);
                                    dsreplay replay = null;
                                    try
                                    {
                                        replay = JsonSerializer.Deserialize<dsreplay>(line);
                                        if (replay != null)
                                        {
                                            DSReplay dsreplay = Map.Rep(replay);
                                            DSPlayer dsplayer = dsreplay.DSPlayer.FirstOrDefault(f => f.NAME == "player");
                                            if (dsplayer != null)
                                                dsplayer.NAME = id;
                                            dsreplay.Upload = player.LastUpload;
                                            DSReplays.Add(dsreplay);
                                            player.Data++;
                                        }
                                    } catch (Exception e)
                                    {
                                        _logger.LogError("Could not Deserialize and map replay " + e.Message);
                                    }
                                }
                            }
                            
                            DSRestUpload upload = new DSRestUpload();
                            upload.Upload = player.LastUpload;
                            upload.DSRestPlayer = player;
                            _context.DSRestUploads.Add(upload);

                            DSinfo info = Infos.FirstOrDefault(f => f.Key == id).Value;
                            if (info != null)
                            {
                                DateTime LastRep = DateTime.MinValue;
                                if (info.LastRep.Length == 14)
                                {
                                    int year = int.Parse(info.LastRep.Substring(0, 4));
                                    int month = int.Parse(info.LastRep.Substring(4, 2));
                                    int day = int.Parse(info.LastRep.Substring(6, 2));
                                    int hour = int.Parse(info.LastRep.Substring(8, 2));
                                    int min = int.Parse(info.LastRep.Substring(10, 2));
                                    int sec = int.Parse(info.LastRep.Substring(12, 2));
                                    LastRep = new DateTime(year, month, day, hour, min, sec);
                                }
                                player.LastRep = LastRep;
                                player.Json = info.Json;
                                player.Total = info.Total;
                                player.Version = info.Version;
                            }
                            
                            lock(dblock)
                            {
                                _context.SaveChanges();
                            }
                            InsertDSReplays();
                            return true;
                        }
                        else return false;
                    });
            }
            else return false;
        }

        public static string Decompress(FileInfo fileToDecompress, string WorkPath, string id, ILogger _logger)
        {
            string newFileName = "";
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                newFileName = WorkPath + "/" + id;
                string tmpFileName = newFileName + ".json";
                int i = 0;
                while (File.Exists(tmpFileName))
                {
                    tmpFileName = WorkPath + "/" + DateTime.Now.ToString("yyyyMMdd") + "_" + i + "_" + Path.GetFileName(newFileName) + ".json";
                    i++;
                }
                newFileName = tmpFileName;

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        try
                        {
                            decompressionStream.CopyTo(decompressedFileStream);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("Failed decompressing " + fileToDecompress.FullName + " to " + newFileName + ": " + e.Message);
                            return "";
                        }
                        Console.WriteLine($"Decompressed: {fileToDecompress.Name}");
                    }
                }
            }
            return newFileName;
        }

        public async Task InsertDSReplays()
        {
            List<DSReplay> replays = new List<DSReplay>();
            lock (lockobject)
            {
                while (true)
                {
                    DSReplay replay = null;
                    DSReplays.TryTake(out replay);
                    if (replay == null)
                        break;
                    else
                        replays.Add(replay);
                }
            }
            if (replays.Any())
                await Task.Run(() => { DbDupFind.ScanRest(replays); });
        }
    }

}