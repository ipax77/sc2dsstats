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
using System.Collections.Concurrent;

namespace sc2dsstats_rc1
{
    class DSmmrest
    {
        public MainWindow MW { get; set; }
        //public static RestClient Client = new RestClient("https://localhost:44393/");
        public static RestClient Client = new RestClient("https://www.pax77.org:9128/");

        public DSmmrest()
        {

        }

        public static BasePlayer LetmePlay(SEplayer player)
        {
            var restRequest = new RestRequest("/mm/letmeplay", Method.POST);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("Authorization", "geheim");
            restRequest.AddJsonBody(player);
            //var response = client.Execute(restRequest);
            try
            {
                var response = Client.Execute<BasePlayer>(restRequest);
                return response.Data;
            } catch {
                return null;
            }
        }

        public static RetFindGame FindGame(string name)
        {
            var restRequest = new RestRequest("/mm/findgame/" + name, Method.GET);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("Authorization", "geheim");
            try
            {
                var response = Client.Execute<RetFindGame>(restRequest);
                return response.Data;
            } catch
            {
                return null;
            }
        }

        public static void ExitQ (string name)
        {
            var restRequest = new RestRequest("/mm/exitq/" + name, Method.GET);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("Authorization", "geheim");
            try
            {
                var response = Client.Execute<RetFindGame>(restRequest);
            }
            catch
            {
            }
        }

        public static MMgame Status(int id)
        {
            var restRequest = new RestRequest("/mm/status/" + id, Method.GET);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("Authorization", "geheim");
            try
            {
                var response = Client.Execute<MMgame>(restRequest);
                return response.Data;
            }
            catch
            {
                return null;
            }
        }

        public static void Accept(string name, int id)
        {
            var restRequest = new RestRequest("/mm/accept/" + name + "/" + id, Method.GET);
            restRequest.AddHeader("Authorization", "geheim");
            try
            {
                var response = Client.Execute(restRequest);
            }
            catch
            {
            }
        }

        public static void Decline(string name, int id)
        {
            var restRequest = new RestRequest("/mm/decline/" + name + "/" + id, Method.GET);
            restRequest.AddHeader("Authorization", "geheim");
            try
            {
                var response = Client.Execute(restRequest);
            }
            catch
            {
            }
        }

        public static void Deleteme(string name)
        {
            var restRequest = new RestRequest("/mm/deleteme/" + name, Method.GET);
            restRequest.AddHeader("Authorization", "geheim");
            try
            {
                var response = Client.Execute(restRequest);
            }
            catch
            {
            }
        }

        public static MMgame Report(dsreplay rep, int id)
        {
            var json = JsonConvert.SerializeObject(rep);

            var restRequest = new RestRequest("/mm/report/" + id, Method.POST);
            restRequest.AddHeader("Authorization", "geheim");
            restRequest.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
            restRequest.AddJsonBody(rep);
            //var response = client.Execute(restRequest);
            try
            {
                var response = Client.Execute<MMgame>(restRequest);
                return response.Data;
            }
            catch
            {
                return null;
            }
        }

        public static void Random(string name)
        {
            var restRequest = new RestRequest("/mm/random/" + name, Method.GET);
            restRequest.AddHeader("Authorization", "geheim");
            try
            {
                var response = Client.Execute(restRequest);
            }
            catch
            {
            }
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
            restRequest.AddHeader("Authorization", "geheim");
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
            restRequest.AddHeader("Authorization", "geheim");
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
            test.AddHeader("Authorization", "geheim");
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
    public class BasePlayer
    {
        public string Name { get; set; }
        public double EXP { get; set; } = 0;
        public double MU { get; set; } = 25;
        public double SIGMA { get; set; } = 25 / 3;
        public int Games { get; set; } = 0;
        public bool Accepted { get; set; } = false;
    }

    [Serializable]
    public class MMplayer : BasePlayer
    {
        public MMgame Game { get; set; }
        public ConcurrentDictionary<MMplayer, byte> Lobby { get; set; } = new ConcurrentDictionary<MMplayer, byte>();
        public int Lobbysize { get; set; } = 0;
        public int Ticks { get; set; } = 0;
        public string Mode { get; set; }
        public string Server { get; set; }
        public string Mode2 { get; set; }
        public bool Random { get; set; } = false;
        

        public MMplayer()
        {

        }

        public MMplayer(string name) : this()
        {
            Name = name;
        }

        public MMplayer(SEplayer sepl) : this()
        {
            Name = sepl.Name;
            Mode = sepl.Mode;
            Server = sepl.Server;
            Mode2 = sepl.Mode2;
        }
    }

    [Serializable]
    public class SEplayer : MMplayer
    {
        public string Mode { get; set; }
        public string Server { get; set; }
        public string Mode2 { get; set; }
        public bool Random { get; set; } = false;
    }

    [Serializable]
    public class RESplayer
    {
        public string Name { get; set; }
        public string Race { get; set; }
        public int Kills { get; set; }
        public int Team { get; set; }
        public int Result { get; set; }
        public int Pos { get; set; }
    }

    [Serializable]
    public class RESgame
    {
        public int Winner { get; set; }
        public DateTime Gametime { get; set; } = DateTime.Now;
        public string Hash { get; set; }
        public List<RESplayer> Players { get; set; } = new List<RESplayer>();
        public List<MMplayer> MMPlayers { get; set; } = new List<MMplayer>();
    }

    [Serializable]
    public class MMgame
    {
        public int ID { get; set; } = 0;
        public DateTime Gametime { get; set; } = DateTime.Now;
        public List<BasePlayer> Team1 { get; set; } = new List<BasePlayer>();
        public List<BasePlayer> Team2 { get; set; } = new List<BasePlayer>();
        public string Hash { get; set; }
        public double Quality { get; set; }
        public string Server { get; set; } = "NA";
        public bool Accepted { get; set; } = false;
        public bool Declined { get; set; } = false;
    }

    [Serializable]
    public class RetFindGame
    {
        public MMgame Game { get; set; }
        public List<BasePlayer> Players { get; set; }
    }
}
