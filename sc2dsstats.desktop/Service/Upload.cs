using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// using RestSharp;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace sc2dsstats.desktop.Service
{
    public class DSrest
    {
        private DBService _db;
        private ILogger _logger;

        public DSrest(DBService db, ILogger<DSrest> logger)
        {
            _db = db;
            
            _logger = logger;
        }

        public async Task<bool> AutoUpload()
        {
            string hash = "UndEsWarSommer";
            string hash2 = "UndEsWarWinter";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string names = String.Join(";", DSdata.Config.Players);
                hash = GetHash(sha256Hash, names);
                hash2 = GetHash(sha256Hash, Program.myJson_file);
            }
            // var client = new RestClient("https://www.pax77.org:9126");

            // DEBUG
            //var client = new RestClient("https://192.168.178.28:9001");
            //var client = new RestClient("http://192.168.178.28:9000");
            //var client = new RestClient("https://localhost:44315");
            // var client = new RestClient("https://localhost:5001");

            HttpClient _http = new HttpClient();
            // _http.BaseAddress = new Uri("https://localhost:5003");
            // _http.BaseAddress = new Uri("https://www.pax77.org:9126");
            _http.BaseAddress = new Uri("https://sc2dsstats.pax77.org");
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSupload77");

            List<DSReplay> UploadReplays = new List<DSReplay>();
            List<dsreplay> UploadReplaysMaped = new List<dsreplay>();
            string lastrep = "";
            string exp_csv = "";
            // RestRequest restRequest = null;
            // IRestResponse response = null;

            if (DSdata.Status.Count > 0)
            {
                DSReplay lrep = null;
                lrep = _db.GetLatestReplay();
                if (lrep != null)
                    lastrep = lrep.GAMETIME.ToString("yyyyMMddHHmmss");
            }

            DSinfo info = new DSinfo();
            info.Name = hash;
            info.Json = hash2;
            info.LastRep = lastrep;
            info.LastUpload = DSdata.Config.LastUpload;
            info.Total = DSdata.Status.Count;
            info.Version = DSdata.DesktopVersion.ToString();

            _logger.LogInformation("Upload: AutoInfo");
            // restRequest = new RestRequest("/secure/data/autoinfo", Method.POST);
            // // restRequest = new RestRequest("api/upload/info", Method.POST);
            // restRequest.RequestFormat = DataFormat.Json;
            // restRequest.AddHeader("Authorization", "DSupload77");
            // restRequest.AddJsonBody(info);
            // response = client.Execute(restRequest);


            var response = await _http.PostAsJsonAsync("secure/data/autoinfo", info);

            _logger.LogInformation($"Upload: autoinfo response: {response.Content} {response.StatusCode}");
            if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = await response.Content.ReadFromJsonAsync<string>();
                if (content.Contains("UpToDate")) return true;
                else lastrep = content;
            }
            else return false;

            lastrep = new String(lastrep.Where(Char.IsDigit).Take(14).ToArray());

            if (!lastrep.Any())
                return false;

            string gametime = lastrep;
            DateTime gtime = DateTime.MinValue;
            if (DSdata.Config.FullSend == false && gametime.Length == 14)
            {
                int year = int.Parse(gametime.Substring(0, 4));
                int month = int.Parse(gametime.Substring(4, 2));
                int day = int.Parse(gametime.Substring(6, 2));
                int hour = int.Parse(gametime.Substring(8, 2));
                int min = int.Parse(gametime.Substring(10, 2));
                int sec = int.Parse(gametime.Substring(12, 2));
                gtime = new DateTime(year, month, day, hour, min, sec);
            }

            UploadReplays = _db.GetUploadReplay(gtime);

            if (!UploadReplays.Any())
                return true;

            List<string> anonymous = new List<string>();

            foreach (DSReplay rep in UploadReplays)
            {
                rep.REPLAYPATH = "";
                foreach (DSPlayer pl in rep.DSPlayer)
                {
                    string plname = pl.NAME;
                    if (DSdata.Config.Players.Contains(pl.NAME)) pl.NAME = "player";
                    else pl.NAME = "player" + pl.REALPOS.ToString();
                }
                string json = "";
                //json = Newtonsoft.Json.JsonConvert.SerializeObject(rep);
                try
                {
                    json = System.Text.Json.JsonSerializer.Serialize(rep);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Upload: {e.Message}");
                }
                anonymous.Add(json);
            }
            exp_csv = Program.workdir + "\\export.json";

            File.WriteAllLines(exp_csv, anonymous);

            string exp_csv_gz = exp_csv + ".gz";
            using (FileStream fileToBeZippedAsStream = new FileInfo(exp_csv).OpenRead())
            {
                using (FileStream gzipTargetAsStream = new FileInfo(exp_csv_gz).Create())
                {
                    using (GZipStream gzipStream = new GZipStream(gzipTargetAsStream, CompressionMode.Compress))
                    {
                        try
                        {
                            fileToBeZippedAsStream.CopyTo(gzipStream);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Upload: {ex.Message}");
                        }
                    }
                }
            }
            // if (DSdata.Config.FullSend == true)
            //     restRequest = new RestRequest("/secure/data/dbfullsend/" + hash);
            // else
            //     restRequest = new RestRequest("/secure/data/dbupload/" + hash);
            // // restRequest = new RestRequest("api/upload/replays/" + hash);
            // restRequest.RequestFormat = DataFormat.Json;
            // restRequest.Method = Method.POST;
            // restRequest.AddHeader("Authorization", "DSupload77");
            // restRequest.AddFile("content", exp_csv_gz);

            // response = client.Execute(restRequest);

            var payload = File.ReadAllBytes(exp_csv_gz);
            MultipartFormDataContent multiContent = new MultipartFormDataContent();
            multiContent.Add(new ByteArrayContent(payload), "files", "upload");

            HttpResponseMessage rresponse;
            if (DSdata.Config.FullSend)
                rresponse = await _http.PostAsync($"secure/data/dbfullsend/{hash}", multiContent);
            else
                rresponse = await _http.PostAsync($"secure/data/dbupload/{hash}", multiContent);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (DSdata.Config.FullSend == true)
                {
                    DSdata.Config.FullSend = false;
                    Status.SaveConfig();
                }
                DSdata.Config.LastUpload = DateTime.UtcNow;
                Status.SaveConfig();
                return true;
            }
            else return false;
        }

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}

