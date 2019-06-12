using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSex2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DSex2.Repositories
{
    public class DataRepository : IDataRepository
    {

        private Dictionary<string, DSplayer> _players = new Dictionary<string, DSplayer>();
        private readonly ILogger _logger;

        private static string WorkDir { get; } = "/data";
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
                    var player = JsonConvert.DeserializeObject<DSplayer>(fileContents);
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
            } else
            {
                _players.Add(id, new DSplayer());
                _players[id].Name = id;
                _players[id]._LastRep = last;
                return "0";
            }
        }

        public async Task<string> Info (DSinfo info)
        {
            if (_players.ContainsKey(info.Name))
            {
                if (IsPlausible(info, _players[info.Name]))
                {
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
            foreach (string line in File.ReadLines(data_json)) {
                var player = JsonConvert.DeserializeObject<DSplayer>(line);
                if (player.Name != id) tmp.Add(line);
                
            }
            tmp.Add(JsonConvert.SerializeObject(_players[id]));
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
                    tmpFileName = WorkPath + "/" + DateTime.Now.ToString("yyyyMMdd") + "_" + i + "_" + Path.GetFileName(tmpFileName);
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
                        } catch (Exception e)
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