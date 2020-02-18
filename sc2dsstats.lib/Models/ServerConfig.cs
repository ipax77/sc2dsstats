using System;

namespace sc2dsstats.lib.Models
{
    public class ServerConfig
    {
        public string MonsterJson { get; set; }
        public string SumDir { get; set; }
        public string SumDir1 { get; set; }
        public string SumDir2 { get; set; }
        public string RESTToken { get; set; }
        public DateTime LastRun { get; set; }
        public string Url { get; set; }
        public string DBConnectionString { get; set; }
    }
}
