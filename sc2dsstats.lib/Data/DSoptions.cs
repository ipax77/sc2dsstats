using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace sc2dsstats.lib.Data
{
    public class DSoptions : INotifyPropertyChanged
    {
        private bool Update_value = false;
        private string Mode_value = String.Empty;
        private bool Player_value = false;
        private bool BeginAtZero_value = false;
        private DSReplay Replay_value = null;

        public int ID { get; set; }
        public int Duration { get; set; } = 5 * 60;
        public int Leaver { get; set; } = 89;
        public int Army { get; set; } = 1500;
        public int Kills { get; set; } = 1500;
        public int Income { get; set; } = 1500;
        public int PlayerCount { get; set; } = 6;

        public DateTime Startdate { get; set; } = new DateTime(2021, 01, 01);
        public DateTime Enddate { get; set; } = DateTime.MinValue;
        public string Time { get; set; } = "This Year";
        public string Vs { get; set; } = String.Empty;

        public bool MengskPreviewFilter { get; set; } = true;

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

            ID = Interlocked.Increment(ref DSdata.OptID);
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
        public HashSet<string> Dataset { get; set; } = new HashSet<string>();
        public string Breakpoint { get; set; } = "";
        public string GameBreakpoint { get; set; } = "MIN10";
        public bool Decoding { get; set; } = false;
        //public DSReplayContext db { get; set; }
        public BuildResult buildResult { get; set; } = new BuildResult();
        public bool OnTheFlyScan { get; set; } = false;
        public bool LatestPatch { get; set; } = false;

        public DSReplay Replay
        {
            get { return this.Replay_value; }
            set
            {
                if (value != this.Replay_value)
                {
                    this.Replay_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

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

        public bool Player
        {
            get { return this.Player_value; }
            set
            {
                if (value != this.Player_value)
                {
                    this.Player_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool BeginAtZero
        {
            get { return this.BeginAtZero_value; }
            set
            {
                if (value != this.BeginAtZero_value)
                {
                    this.BeginAtZero_value = value;
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
            opthash += Duration;
            opthash += Leaver + Army + Kills + Income;
            opthash += Startdate.ToString("yyyyMMdd");
            opthash += Enddate.ToString("yyyyMMdd");
            opthash += Interest;
            opthash += Vs;
            opthash += Player;
            opthash += String.Join("", Dataset.OrderBy(o => o));
            opthash += String.Join("", Gamemodes.Where(x => x.Value == true).OrderBy(o => o.Key).Select(s => s.Key));
            opthash += String.Join("", Players.Where(x => x.Value == true).OrderBy(o => o.Key).Select(s => s.Key));
            opthash += Breakpoint;
            opthash += MengskPreviewFilter;
            Hash = opthash;
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
            this.Time = defoptions.Time;
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
            this.GameBreakpoint = defoptions.GameBreakpoint;
            this.PlayerCount = defoptions.PlayerCount;
            this.Mode = Winrate;
            this.MengskPreviewFilter = defoptions.MengskPreviewFilter;
        }
    }

    public class RadioCheckString : INotifyPropertyChanged
    {
        private string Selected_value = String.Empty;
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Selected
        {
            get { return this.Selected_value; }
            set
            {
                if (value != this.Selected_value)
                {
                    this.Selected_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }

    public class RadioCheckBool : INotifyPropertyChanged
    {
        private bool Selected_value = false;
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private DSoptions options { get; set; }

        public RadioCheckBool(DSoptions _options)
        {
            options = _options;
        }

        public bool Selected
        {
            get { return this.Selected_value; }
            set
            {
                if (value != this.Selected_value)
                {
                    this.Selected_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
