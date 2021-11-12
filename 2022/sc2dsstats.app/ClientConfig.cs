using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace sc2dsstats.app
{
    public class OldUserConfig
    {
        public string WorkDir { get; set; }
        public string ExeDir { get; set; }
        public List<string> Players { get; set; }
        public List<string> Replays { get; set; }
        public int Cores { get; set; }
        public bool Autoupdate { get; set; }
        public bool Autoscan { get; set; }
        public bool Autoupload { get; set; }
        public bool Autoupload_v1_1_10 { get; set; }
        public bool Uploadcredential { get; set; }
        public bool MMcredential { get; set; }
        public string Version { get; set; }
        public DateTime LastUpload { get; set; }
        public DateTime MMDeleted { get; set; }
        public bool NewVersion1_4_1 { get; set; }
        public bool FullSend { get; set; }
        public bool OnTheFlyScan { get; set; }
        public int Debug { get; set; }
        public string Auth { get; set; }
    }

    public class OldConfig
    {
        public OldUserConfig Config { get; set; }
    }

    public class AppConfig
    {
        public UserConfig Config { get; set; }
    }

    [Serializable]
    public class UserConfig
    {
        public Guid AppId { get; set; }
        public Guid DbId { get; set; }
        public string DisplayName { get; set; }
        public List<string> PlayersNames { get; set; }
        public List<string> ReplayPaths { get; set; }
        public bool Uploadcredential { get; set; }
        public bool OnTheFlyScan { get; set; }
        public int CPUCores { get; set; }
        public int DebugLevel { get; set; }
        public int CredentialAsking { get; set; }
        [JsonIgnore]
        public List<EditEnt> PlayerEnts { get; set; }
        [JsonIgnore]
        public List<EditEnt> PathEnts { get; set; }
    }

    public class EditEnt
    {
        public string ent { get; set; }
    }
}
