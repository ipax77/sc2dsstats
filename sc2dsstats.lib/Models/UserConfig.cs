using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

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

        public UserConfig()
        {

        }

        public UserConfig(UserConfigDb cp) : this()
        {
            this.WorkDir = cp.WorkDir;
            this.ExeDir = cp.ExeDir;
            this.Players = new List<string>();
            this.Replays = new List<string>();

            foreach (UserConfigDbPlayers pl in cp.Players.OrderBy(o => o.Player))
                this.Players.Add(pl.Player);
            foreach (UserConfigDbReplays rep in cp.Replays)
                this.Replays.Add(rep.Replay);

            this.Cores = cp.Cores;
            this.Autoupdate = cp.Autoupdate;
            this.Autoscan = cp.Autoscan;
            this.Autoupload = cp.Autoupload;
            this.Autoupload_v1_1_10 = cp.Autoupload_v1_1_10;
            this.Uploadcredential = cp.Uploadcredential;
            this.MMcredential = cp.MMcredential;
            this.Version = cp.Version;
            this.LastUpload = cp.LastUpload;
            this.MMDeleted = cp.MMDeleted;
            this.NewVersion1_4_1 = cp.NewVersion1_4_1;
            this.FullSend = cp.FullSend;
            this.OnTheFlyScan = cp.OnTheFlyScan;
            this.Debug = cp.Debug;
            this.Auth = cp.Auth;
        }


    }

    public class UserConfigDb
    {
        public string WorkDir { get; set; }
        public string ExeDir { get; set; }
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

        public ICollection<UserConfigDbPlayers> Players { get; set; }
        [Required]
        [MinLength(1)]
        public ICollection<UserConfigDbReplays> Replays { get; set; }

        public UserConfigDb()
        {

        }

        public UserConfigDb(UserConfig cp) : this()
        {
            this.WorkDir = cp.WorkDir;
            this.ExeDir = cp.ExeDir;
            List<UserConfigDbPlayers> players = new List<UserConfigDbPlayers>();
            List<UserConfigDbReplays> replays = new List<UserConfigDbReplays>();
            foreach (string player in cp.Players)
            {
                UserConfigDbPlayers pl = new UserConfigDbPlayers();
                pl.Player = player;
                players.Add(pl);
            }
            foreach (string replay in cp.Replays)
            {
                UserConfigDbReplays rep = new UserConfigDbReplays();
                rep.Replay = replay;
                replays.Add(rep);
            }
            this.Players = players;
            this.Replays = replays;
            this.Cores = cp.Cores;
            this.Autoupdate = cp.Autoupdate;
            this.Autoscan = cp.Autoscan;
            this.Autoupload = cp.Autoupload;
            this.Autoupload_v1_1_10 = cp.Autoupload_v1_1_10;
            this.Uploadcredential = cp.Uploadcredential;
            this.MMcredential = cp.MMcredential;
            this.Version = cp.Version;
            this.LastUpload = cp.LastUpload;
            this.MMDeleted = cp.MMDeleted;
            this.NewVersion1_4_1 = cp.NewVersion1_4_1;
            this.FullSend = cp.FullSend;
            this.OnTheFlyScan = cp.OnTheFlyScan;
            this.Debug = cp.Debug;
            this.Auth = cp.Auth;
        }

        public static ValidationResult ValidateReplaypath(string replay, ValidationContext vc)
        {
            return Directory.Exists(replay)
                ? ValidationResult.Success
                : new ValidationResult("Directory not found!", new[] { vc.MemberName });
        }
    }

    public class UserConfigDbPlayers
    {
        [Required]
        [MaxLength(32)]
        public string Player { get; set; } = String.Empty;
        public bool isValidB { get; set; } = true;
        public ValidationResult isValid { get; set; }

        public bool Check()
        {
            if (String.IsNullOrEmpty(Player) || Player.Any(char.IsDigit))
                isValidB = false;
            else if (Player.Length > 32 || Player.Length < 3)
                isValidB = false;
            else
                isValidB = true;
            return isValidB;
        }
    }
    public class UserConfigDbReplays
    {
        [CustomValidation(typeof(UserConfigDb), nameof(UserConfigDb.ValidateReplaypath))]
        public string Replay { get; set; } = String.Empty;
        public bool isValidB { get; set; } = true;
        public ValidationResult isValid { get; set; }

        public bool Check()
        {
            if (String.IsNullOrEmpty(Replay))
                return false;
            if (Directory.Exists(Replay))
                isValidB = true;
            else
                isValidB = false;
            return isValidB;
        }
    }

}
