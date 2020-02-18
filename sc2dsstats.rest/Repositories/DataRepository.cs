using Microsoft.Extensions.Logging;
using sc2dsstats.rest.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.rest.Repositories
{
    public class DataRepository : IDataRepository
    {

        private Dictionary<string, DSplayer> _players = new Dictionary<string, DSplayer>();
        private readonly ILogger _logger;

        private static string WorkDir { get; } = "/data";
        private static string SharedDir { get; } = "/autodata";
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public DataRepository(ILogger<DataRepository> logger)
        {
            _logger = logger;
            string data_json = WorkDir + "/data.json";
            if (File.Exists(data_json))
            {
                TextReader reader = new StreamReader(data_json, Encoding.UTF8);
                string fileContents;
                while ((fileContents = reader.ReadLine()) != null)
                {
                    var player = JsonSerializer.Deserialize<DSplayer>(fileContents);
                    if (player != null)
                    {
                        _players.Add(player.Name, player);
                    }
                }
                reader.Close();
            }
        }

        public string GetLast(string id, string last)
        {
            if (_players.ContainsKey(id))
            {
                if (_players[id].LastRep == last) return "UpToDate";
                _players[id]._LastRep = last;
                return (_players[id].LastRep);
            }
            else
            {
                _players.Add(id, new DSplayer());
                _players[id].Name = id;
                _players[id]._LastRep = last;
                return "0";
            }
        }

        public async Task<string> Info(DSinfo info)
        {
            if (_players.ContainsKey(info.Name))
            {
                if (IsPlausible(info, _players[info.Name]))
                {
                    _players[info.Name].Info = info;
                    if (info.Version.StartsWith("v"))
                        info.Version = info.Version.Substring(1);
                    if (_players[info.Name].SendAllV1_5 == false && new Version(info.Version).CompareTo(new Version("1.1.5")) >= 0)
                        return "0";

                    if (_players[info.Name].LastRep == info.LastRep) return "UpToDate";
                    _players[info.Name]._LastRep = info.LastRep;
                    _players[info.Name].Info = info;
                    return (_players[info.Name].LastRep);
                }
                else return "0";
            }
            else
            {
                _players.Add(info.Name, new DSplayer());
                _players[info.Name].Name = info.Name;
                _players[info.Name]._LastRep = info.LastRep;
                _players[info.Name].Info = info;
                return "0";
            }
        }

        public async Task<string> AutoInfo(DSinfo info)
        {
            string mypath = SharedDir + "/" + info.Name;
            string mysum = SharedDir + "/sum/" + info.Name + ".json";

            string myreturn = "0";

            if (_players.ContainsKey(info.Name))
            {
                if (_players[info.Name].LastAutoRep == info.LastRep) myreturn = "UpToDate";
                else
                {
                    _players[info.Name]._LastAutoRep = info.LastRep;
                    _players[info.Name].Info = info;
                    myreturn = _players[info.Name].LastAutoRep;
                }
            }
            else
            {
                _players.Add(info.Name, new DSplayer());
                _players[info.Name].Name = info.Name;
                _players[info.Name]._LastRep = info.LastRep;
                _players[info.Name].Info = info;
                myreturn = "0";
            }

            if (!File.Exists(mysum) || new FileInfo(mysum).Length == 0)
                return "0";
            else
                return myreturn;
        }

        public async Task<bool> GetFile(string id, string myfile)
        {
            DateTime t = new DateTime();
            t = DateTime.Now;
            if (!_players.ContainsKey(id))
            {
                return false;
            }

            string mypath = WorkDir + "/" + id;
            string mysum = WorkDir + "/sum/" + id + ".json";
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
            if (File.Exists(myfile))
            {
                if (!File.Exists(mysum)) File.Create(mysum).Dispose();
                string myjson = "";
                return await Task.Run(() => myjson = Decompress(new FileInfo(myfile), mypath, id, _logger))
                    .ContinueWith(task =>
                    {
                        if (myjson == "") return false;
                        if (File.Exists(myjson))
                        {
                            if (_players[id].Info.Version.StartsWith("v"))
                                _players[id].Info.Version = _players[id].Info.Version.Substring(1);

                            if (_players[id].SendAllV1_5 == false && new Version(_players[id].Info.Version).CompareTo(new Version("1.1.5")) >= 0)
                            {
                                string dest = WorkDir + "/bak/" + Path.GetFileName(mysum);
                                if (File.Exists(dest))
                                    File.Delete(dest);

                                if (File.Exists(mysum))
                                    File.Move(mysum, dest);

                                File.Create(mysum).Dispose();
                                _players[id].Data = 0;
                                _players[id].SendAllV1_5 = true;
                                _players[id].Info.SendAllV1_5 = true;
                            }

                            using (StreamWriter sw = File.AppendText(mysum))
                            {
                                foreach (string line in File.ReadLines(myjson))
                                {
                                    if (!line.StartsWith(@"{")) return false;
                                    sw.WriteLine(line);
                                    _players[id].Data++;
                                }
                            }
                            _players[id].LastRep = _players[id]._LastRep;
                            _players[id].Uploads.Add(t);
                            SaveInfo(id);
                            return true;
                        }
                        else return false;
                    });
            }
            else return false;
        }

        public async Task<bool> GetAutoFile(string id, string myfile)
        {
            DateTime t = new DateTime();
            t = DateTime.Now;

            string mypath = SharedDir + "/" + id;
            string mysum = SharedDir + "/sum/" + id + ".json";
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
                                    _players[id].Data++;
                                }
                            }
                            _players[id].LastAutoRep = _players[id]._LastAutoRep;
                            _players[id].Uploads.Add(t);
                            SaveInfo(id);
                            return true;
                        }
                        else return false;
                    });
            }
            else return false;
        }

        public async Task<bool> FullSend(string id, string myfile)
        {
            DateTime t = new DateTime();
            t = DateTime.Now;

            string mypath = SharedDir + "/" + id;
            string mysum = SharedDir + "/sum/" + id + ".json";
            string mybak = SharedDir + "/bak/" + id + ".json";
            int i = 0;
            while (File.Exists(mybak))
            {
                mybak = SharedDir + "/bak/" + id + "_" + i + ".json";
                i++;
            }

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
                            File.Move(mysum, mybak);
                            File.Copy(myjson, mysum);
                            _players[id].LastAutoRep = _players[id]._LastAutoRep;
                            _players[id].Uploads.Add(t);
                            SaveInfo(id);
                            return true;
                        }
                        else return false;
                    });
            }
            else return false;
        }

        public bool IsPlausible(DSinfo Info, DSplayer Player)
        {
            int plausible = 0;
            if (Player.Uploads.Count > 0)
            {
                TimeSpan valid = new TimeSpan(0, 6, 0, 0);
                TimeSpan t = Info.LastUpload.Subtract(Player.Uploads[Player.Uploads.Count - 1]);
                if (t.Duration().CompareTo(valid) > 0) plausible--;
                else plausible++;
            }

            if (Info.Total < Player.Data) plausible--;
            else plausible++;

            if (Info.Json == Player.Info.Json) plausible++;
            else plausible--;

            if (plausible > 0) return true;
            _logger.LogWarning("Not plausible {0}: {1}", plausible, Info.Name);
            return false;
        }

        public void SaveInfo(string id)
        {
            string data_json = WorkDir + "/data.json";
            List<string> tmp = new List<string>();
            foreach (string line in File.ReadLines(data_json))
            {
                var player = JsonSerializer.Deserialize<DSplayer>(line);
                if (player.Name != id) tmp.Add(line);

            }
            tmp.Add(JsonSerializer.Serialize(_players[id]));
            _readWriteLock.EnterWriteLock();
            if (File.Exists(data_json + "_bak"))
            {
                File.Delete(data_json + "_bak");
            }
            File.Move(data_json, data_json + "_bak");
            File.WriteAllLines(data_json, tmp);
            _readWriteLock.ExitWriteLock();
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
    }
}