using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using sc2dsstats.lib.Models;

namespace sc2dsstats.lib.Data
{
    public class DSoptions : INotifyPropertyChanged
    {
        private bool Update_value = false;
        private string Mode_value = String.Empty;

        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
        public int Leaver { get; set; } = 2000;
        public int Army { get; set; } = 1500;
        public int Kills { get; set; } = 1500;
        public int Income { get; set; } = 1500;
        public int PlayerCount { get; set; } = 6;
        public bool Player { get; set; } = false;
        public DateTime Startdate { get; set; } = new DateTime(2020, 01, 01);
        public DateTime Enddate { get; set; } = DateTime.MinValue;
        public string Vs { get; set; } = String.Empty;
        public bool BeginAtZero { get; set; } = false;
        public string Build { get; set; } = String.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DSoptions()
        {
            foreach (var ent in DSdata.s_gamemodes)
            {
                Gamemodes.Add(ent, false);
            }
            Gamemodes["GameModeCommanders"] = true;
            Gamemodes["GameModeCommandersHeroic"] = true;

            foreach (var ent in DSdata.Config.Players)
                this.Players[ent] = true;

            foreach (var ent in DSdata.s_races)
                this.CmdrsChecked[ent] = false;

            Hash = GenHash();
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public string Interest { get; set; } = "";
        public CmdrInfo Cmdrinfo { get; set; } = new CmdrInfo();
        public ChartJS Chart { get; set; } = new ChartJS();
        public Dictionary<string, bool> Gamemodes { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> Players { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> CmdrsChecked { get; set; } = new Dictionary<string, bool>();
        public string Hash { get; set; } = "";
        public string Dataset { get; set; } = "";
        public string Breakpoint { get; set; } = "";

        public bool Update
        {
            get { return this.Update_value; }
            set
            {
                if (value != this.Update_value)
                {
                    this.Update_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Mode
        {
            get { return this.Mode_value; }
            set
            {
                if (value != this.Mode_value)
                {
                    this.Mode_value = value;
                    NotifyPropertyChanged();
                }
            }
        }


        public List<string> GetGamemodes()
        {
            return Gamemodes.Where(x => x.Value == true).Select(s => s.Key).ToList();
        }

        public List<string> GetCmdrsChecked()
        {
            return CmdrsChecked.Where(x => x.Value == true).Select(s => s.Key).ToList();
        }

        public string GenHash()
        {
            string opthash = "";

            opthash += Mode;
            opthash += Build;
            opthash += Duration.TotalMinutes;
            opthash += Leaver + Army + Kills + Income;
            opthash += Startdate.ToString("yyyyMMdd");
            opthash += Enddate.ToString("yyyyMMdd");
            opthash += Interest;
            opthash += Vs;
            opthash += Player;
            opthash += Dataset;
            opthash += String.Join("", Gamemodes.Where(x => x.Value == true).OrderBy(o => o.Key).Select(s => s.Key));
            opthash += String.Join("", Players.Where(x => x.Value == true).OrderBy(o => o.Key).Select(s => s.Key));
            opthash += Breakpoint;

            return opthash;
        }

        public void DefaultFilter(string Winrate = "")
        {
            DSoptions defoptions = new DSoptions();
            this.Build = String.Empty;
            this.Duration = defoptions.Duration;
            this.Leaver = defoptions.Leaver;
            this.Army = defoptions.Army;
            this.Kills = defoptions.Kills;
            this.Income = defoptions.Income;
            this.Startdate = defoptions.Startdate;
            this.Enddate = defoptions.Enddate;
            this.Interest = defoptions.Interest;
            this.Vs = defoptions.Vs;
            this.Player = defoptions.Player;
            this.Dataset = defoptions.Dataset;
            this.Gamemodes = new Dictionary<string, bool>(defoptions.Gamemodes);
            this.Players = new Dictionary<string, bool>();
            foreach (var ent in DSdata.Config.Players)
                this.Players[ent] = true;
            this.CmdrsChecked = new Dictionary<string, bool>(defoptions.CmdrsChecked);
            this.Cmdrinfo = new CmdrInfo();
            this.Breakpoint = defoptions.Breakpoint;
            this.PlayerCount = defoptions.PlayerCount;
            this.Mode = Winrate;
        }
    }
}
