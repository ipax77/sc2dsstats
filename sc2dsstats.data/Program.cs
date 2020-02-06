using Microsoft.Extensions.Configuration;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace sc2dsstats.data
{
    class Program
    {
        static HttpClient client = new HttpClient();
        public static ServerConfig Config = new ServerConfig();
        public static string configfile = "/home/pax77/git/config/serverconfig.json";

        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile(configfile, true, true)
              .Build();
            config.GetSection("ServerConfig").Bind(Config);

            Scan2.Scan();
            //SendUpdateRequest();
        }

        public static void SaveConfig()
        {
            Dictionary<string, ServerConfig> temp = new Dictionary<string, ServerConfig>();
            temp.Add("ServerConfig", Config);

            var option = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(temp, option);
            File.WriteAllText(configfile, json);
        }

        public static void SendUpdateRequest()
        {
            client.BaseAddress = new Uri(Config.Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Config.RESTToken);
            Console.WriteLine("Sending Request");
            HttpResponseMessage response = client.GetAsync(Config.Url + "/secure/reload").GetAwaiter().GetResult();
        }
    }
}
