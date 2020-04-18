using System;
using System.Collections.Generic;

namespace sc2dsstats.lib.Models
{
    public class UserConfig
    {
        public string WorkDir { get; set; }
        public string ExeDir { get; set; }
        public List<string> Players { get; set; } = new List<string>();
        public List<string> Replays { get; set; } = new List<string>();
        public int Cores { get; set; } = 2;
        public bool Autoupdate { get; set; } = false;
        public bool Autoscan { get; set; } = false;
        public bool Autoupload { get; set; } = false;
        public bool Autoupload_v1_1_10 { get; set; } = true;
        public bool Uploadcredential { get; set; } = false;
        public bool MMcredential { get; set; } = false;
        public string Version { get; set; } = "0.7";
        public DateTime LastUpload { get; set; } = new DateTime(2018, 1, 1);
        public DateTime MMDeleted { get; set; } = new DateTime(2018, 1, 1);
        public bool NewVersion1_4_1 { get; set; } = true;
        public bool FullSend { get; set; } = false;
        public bool OnTheFlyScan { get; set; } = true;
        public int Debug { get; set; } = 0;
        public string Auth { get; set; } = "DSupload77";
    }
}
