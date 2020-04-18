using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace sc2dsstats.data
{
    class Program
    {
        static HttpClient client = new HttpClient();
        public static string configfile = "/home/pax77/git/config/localserverconfig.json";

        public static DbContextOptions<DSReplayContext> _opt;
        private static bool isInit = false;

        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

        static void Main(string[] args)
        {
            Init();
            int i = 0;
            DSdata.Init();
            List<DSReplay> replays = new List<DSReplay>();
            using (var context = new DSReplayContext(_opt))
            {
                context.Database.EnsureCreated();
                i = context.DSReplays.Count();
                //replays = context.DSReplays.Include(p => p.DSPlayer).ToList();
            }

            DateTime t = DateTime.Now;

            //int done = 0;
            int done = DbDupFind.ScanAdd();
            //int done = DbDupFind.ScanNG();

            if (done > 0)
                Program.SendUpdateRequest();

            DSdata.ServerConfig.LastRun = t;
            Program.SaveConfig();
        }

        public static void Init()
        {
            if (isInit)
                return;
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile(configfile, true, true)
              .Build();
            config.GetSection("ServerConfig").Bind(DSdata.ServerConfig);

            var optionsBuilder = new DbContextOptionsBuilder<DSReplayContext>();
            optionsBuilder
                //.EnableSensitiveDataLogging()
                //.UseLoggerFactory(MyLoggerFactory)
                .UseMySql(DSdata.ServerConfig.DBConnectionString, mySqlOptions => mySqlOptions
                .ServerVersion(new ServerVersion(new Version(5, 7, 29), ServerType.MySql)));

            _opt = optionsBuilder.Options;
            isInit = true;
        }


        public static void SaveConfig()
        {
            Dictionary<string, ServerConfig> temp = new Dictionary<string, ServerConfig>();
            temp.Add("ServerConfig", DSdata.ServerConfig);

            var option = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize(temp, option);
            File.WriteAllText(configfile, json);
        }

        public static void SendUpdateRequest()
        {
            client.BaseAddress = new Uri(DSdata.ServerConfig.Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(DSdata.ServerConfig.RESTToken);
            Console.WriteLine("Sending Request");
            try
            {
                HttpResponseMessage response = client.GetAsync(DSdata.ServerConfig.Url + "/secure/reload").GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
