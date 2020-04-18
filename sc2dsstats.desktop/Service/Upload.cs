using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RestSharp;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using sc2dsstats.decode.Models;
using sc2dsstats.decode.Service;
using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Service;
using Newtonsoft.Json;

namespace sc2dsstats.desktop.Service
{
    public static class DSrest
    {
        public static bool AutoUpload(DSReplayContext _context)
        {
            string hash = "UndEsWarSommer";
            string hash2 = "UndEsWarWinter";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string names = String.Join(";", DSdata.Config.Players);
                hash = GetHash(sha256Hash, names);
                hash2 = GetHash(sha256Hash, Program.myJson_file);
            }
            //var client = new RestClient("https://www.pax77.org:9126");
            //var client = new RestClient("https://192.168.178.28:9001");
            //var client = new RestClient("http://192.168.178.28:9000");
            var client = new RestClient("https://localhost:44315");
            //var client = new RestClient("http://localhost:5000");

            List<DSReplay> UploadReplays = new List<DSReplay>();
            List<dsreplay> UploadReplaysMaped = new List<dsreplay>();
            string lastrep = "";
            if (DSdata.Status.Count > 0)
            {
                var lrep = _context.DSReplays.OrderByDescending(o => o.GAMETIME).Take(1);

                //_context.DSReplays.OrderByDescending(o => o.GAMETIME).First();
                lastrep = lrep.First().GAMETIME.ToString("yyyyMMddhhmmss");
            }

            DSinfo info = new DSinfo();
            info.Name = hash;
            info.Json = hash2;
            info.LastRep = lastrep;
            info.LastUpload = DSdata.Config.LastUpload;
            info.Total = DSdata.Status.Count;
            info.Version = DSdata.DesktopVersion.ToString();

            Console.WriteLine("AutoInfo");
            var restRequest = new RestRequest("/secure/data/autoinfo", Method.POST);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("Authorization", "DSupload77");
            restRequest.AddJsonBody(info);
            var response = client.Execute(restRequest);

            Console.WriteLine(response.Content);
            if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response.Content.Contains("UpToDate")) return true;
                else lastrep = response.Content;
            }
            else return false;

            lastrep = new String(lastrep.Where(Char.IsDigit).Take(14).ToArray());

            if (!lastrep.Any())
                return false;

            string gametime = lastrep;
            DateTime gtime = DateTime.MinValue;
            if (gametime.Length == 14) {
                int year = int.Parse(gametime.Substring(0, 4));
                int month = int.Parse(gametime.Substring(4, 2));
                int day = int.Parse(gametime.Substring(6, 2));
                int hour = int.Parse(gametime.Substring(8, 2));
                int min = int.Parse(gametime.Substring(10, 2));
                int sec = int.Parse(gametime.Substring(12, 2));
                gtime = new DateTime(year, month, day, hour, min, sec);
            }

            DSdata.Config.FullSend = true;
            if (gtime == DateTime.MinValue || DSdata.Config.FullSend == true)
                UploadReplays = _context.DSReplays
                    .Include(o => o.Middle)
                    .Include(p => p.DSPlayer)
                    .ThenInclude(p => p.Breakpoints)
                    .ToList();
            else
                UploadReplays = _context.DSReplays
                    .Include(o => o.Middle)
                    .Include(p => p.DSPlayer)
                    .ThenInclude(p => p.Breakpoints).Where(x => x.GAMETIME > gtime)
                    .ToList();
            DSdata.Config.FullSend = false;
            List<string> anonymous = new List<string>();
            foreach (DSReplay rep in UploadReplays)
            {
                rep.REPLAYPATH = "";
                rep.Upload = DateTime.UtcNow;
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
                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                anonymous.Add(json);
            }
            string exp_csv = Program.workdir + "\\export.json";

            File.WriteAllLines(exp_csv, anonymous);

            _context.ChangeTracker.Entries()
                .Where(e => e.Entity != null).ToList()
                .ForEach(e => e.State = EntityState.Detached);

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
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            if (DSdata.Config.FullSend == true)
                restRequest = new RestRequest("/secure/data/dbfullsend/" + hash);
            else
                restRequest = new RestRequest("/secure/data/dbupload/" + hash);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.Method = Method.POST;
            restRequest.AddHeader("Authorization", "DSupload77");
            restRequest.AddFile("content", exp_csv_gz);
            response = client.Execute(restRequest);
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

