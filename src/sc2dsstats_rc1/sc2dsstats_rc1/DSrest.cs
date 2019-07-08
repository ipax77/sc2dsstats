using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace sc2dsstats_rc1
{
    class DSrest
    {
        public MainWindow MW { get; set; }

        public DSrest()
        {

        }

        public bool Upload()
        {
            string hash = "UndEsWarSommer";
            string hash2 = "UndEsWarWinter";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                hash = MainWindow.GetHash(sha256Hash, MW.player_name);
                hash2 = MainWindow.GetHash(sha256Hash, MW.myStats_json);
            }
            var client = new RestClient("https://www.pax77.org:9126");
            //var client = new RestClient("https://192.168.178.28:9001");
            //var client = new RestClient("http://192.168.178.28:9000");
            //var client = new RestClient("https://localhost:44393");

            List<dsreplay> temp = new List<dsreplay>(MW.replays);
            string lastrep = "";
            if (temp.Count > 0)
            {
                lastrep = temp.OrderByDescending(o => o.GAMETIME).ElementAt(0).GAMETIME.ToString().Substring(0, 14);
            }

            DSinfo info = new DSinfo();
            info.Name = hash;
            info.Json = hash2;
            info.LastRep = lastrep;
            info.LastUpload = Properties.Settings.Default.UPLOAD;
            info.Total = MW.replays.Count();
            MW.Dispatcher.Invoke(() =>
            {
                info.Version = MW.lb_sb_info0.Content.ToString();
            });

            var restRequest = new RestRequest("/secure/data/info", Method.POST);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("Authorization", "DSupload77");
            restRequest.AddJsonBody(info);
            var response = client.Execute(restRequest);

            if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response.Content.Contains("UpToDate")) return true;
                else lastrep = response.Content;
            }
            else return false;

            lastrep = new String(lastrep.Where(Char.IsDigit).Take(14).ToArray());

            double dlastrep = 0;
            try
            {
                dlastrep = Double.Parse(lastrep);
            }
            catch
            {
                return false;
            }
            if (dlastrep == 0) temp = new List<dsreplay>(MW.replays);
            else temp = new List<dsreplay>(MW.replays.Where(x => x.GAMETIME > dlastrep).ToList());

            List<string> anonymous = new List<string>();
            foreach (dsreplay replay in temp)
            {
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    if (MW.player_list.Contains(pl.NAME)) pl.NAME = "player";
                    else pl.NAME = "player" + pl.REALPOS.ToString();
                }
                anonymous.Add(JsonConvert.SerializeObject(replay));
            }
            string exp_csv = MW.myAppData_dir + "\\export.json";
            if (!File.Exists(exp_csv))
            {
                File.Delete(exp_csv);
            }
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
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            restRequest = new RestRequest("/secure/data/upload/" + hash);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.Method = Method.POST;
            restRequest.AddHeader("Authorization", "DSupload77");
            restRequest.AddFile("content", exp_csv_gz);
            response = client.Execute(restRequest);
            if (response.StatusCode == System.Net.HttpStatusCode.OK) return true;
            else return false;

        }

        public void Rest()
        {
            
            var client = new RestClient("http://192.168.178.28:9000");
            //var client = new RestClient("http://127.0.0.1:44373");
            // client.Authenticator = new HttpBasicAuthenticator(username, password);


            var request = new RestRequest("secure/person/all", Method.GET);
            //request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
            //request.AddUrlSegment("id", "123"); // replaces matching token in request.Resource

            // easily add HTTP Headers
            request.AddHeader("Authorization", "value");

            // add files to upload (works with compatible verbs)
            //request.AddFile(path);

            // execute the request
            IRestResponse response = client.Execute(request);
            var content = response.Content; // raw content as string

            Person myperson = new Person();
            myperson.FirstName = "bab";
            myperson.LastName = "zach";
            myperson.Email = "bab@zack.org";
            var postit = new RestRequest("person/save", Method.POST);
            //postit.AddBody(myperson);
            postit.AddJsonBody(myperson);
            response = client.Execute(postit);
            
            response = client.Execute(request);
            
            RestRequest restRequest = new RestRequest("person/upload");
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.Method = Method.POST;
            restRequest.AddHeader("Authorization", "geheim");
            restRequest.AddFile("content", "C:/temp/bab/data.json");
            //response = client.Execute(restRequest);
            

            RestRequest test = new RestRequest("secure/data/last/winter");
            test.Method = Method.GET;
            test.AddHeader("Authorization", "DSupload77");
            response = client.Execute(test);
            
            Console.WriteLine("indhouse");
            // or automatically deserialize result
            // return content type is sniffed but can be explicitly set via RestClient.AddHandler();
            //RestResponse<Person> response2 = client.Execute<Person>(request);
            //var name = response2.Data.Name;

            // easy async support
            //client.ExecuteAsync(request, response => {
            //    Console.WriteLine(response.Content);
            //});

            // async with deserialization
            //var asyncHandle = client.ExecuteAsync<Person>(request, response => {
            //    Console.WriteLine(response.Data.Name);
            //});

            // abort the request on demand
            //asyncHandle.Abort();
        }

    }

    [Serializable]
    public class DSinfo
    {
        public string Name { get; set; }
        public string Json { get; set; }
        public int Total { get; set; }
        public DateTime LastUpload { get; set; }
        public string LastRep { get; set; }
        public string Version { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}
