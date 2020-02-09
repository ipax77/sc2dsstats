using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using sc2dsstats.lib.Models;
using System;

namespace sc2dsstats.lib
{
    public class sc2dsstatslib
    {
        //public static string JsonFile { get; set; } = @"C:\Users\pax77\AppData\Local\sc2dsstats_web\data.json";
        public static string JsonFile { get; set; } = "/data/data.json";
        public static ServerConfig Config = new ServerConfig();
        public static string configfile = "/home/pax77/git/config/serverconfig.json";
        

        public static void LoadConfig()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile(configfile, true, true)
                .Build();
                config.GetSection("ServerConfig").Bind(Config);
        }
    }
}
