using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using sc2dsstats.lib.Models;

namespace sc2dsstats.lib.Data
{
    public class DSoptions : INotifyPropertyChanged
    {
        private int Duration_value = 5376;
        private int Leaver_value = 2000;
        private int Army_value = 1500;
        private int Kills_value = 1500;
        private int Income_value = 1500;
        private int PlayerCount_value = 6;
        private bool Player_value = false;
        private DateTime Startdate_value = new DateTime(2020, 01, 01);
        private DateTime Enddate_value = DateTime.Now.AddDays(1);
        private string Interest_value = String.Empty;
        private string Vs_value = String.Empty;
        private bool Matchup_value = false;
        private bool Filter_value = false;
        private string Mode_value = "Winrate";
        private bool BeginAtZero_value = false;
        private string Build_value = String.Empty;
        private string Gamemode_value = "Commanders";
        private bool Update_value = false;

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

            Cmdrinfo.Add("ALL", new CmdrInfo());

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

        public int Icons { get; set; } = 0;
        public bool OPT { get; set; } = false;
        public bool DOIT { get; set; } = true;
        public Dictionary<string, CmdrInfo> Cmdrinfo { get; set; } = new Dictionary<string, CmdrInfo>();
        public ChartJS Chart { get; set; } = new ChartJS();
        public int Total { get; set; } = 0;
        public Dictionary<string, bool> Gamemodes { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> Players { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> CmdrsChecked { get; set; } = new Dictionary<string, bool>();
        public string Hash { get; set; } = "";
        public string Dataset { get; set; } = "";

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

        public int Duration
        {
            get { return this.Duration_value; }
            set
            {
                if (value != this.Duration_value)
                {
                    this.Duration_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Army
        {
            get { return this.Army_value; }
            set
            {
                if (value != this.Army_value)
                {
                    this.Army_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Leaver
        {
            get { return this.Leaver_value; }
            set
            {
                if (value != this.Leaver_value)
                {
                    this.Leaver_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Kills
        {
            get { return this.Kills_value; }
            set
            {
                if (value != this.Kills_value)
                {
                    this.Kills_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Income
        {
            get { return this.Income_value; }
            set
            {
                if (value != this.Income_value)
                {
                    this.Income_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int PlayerCount
        {
            get { return this.PlayerCount_value; }
            set
            {
                if (value != this.PlayerCount_value)
                {
                    this.PlayerCount_value = value;
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
        public DateTime Startdate
        {
            get { return this.Startdate_value; }
            set
            {
                if (value != this.Startdate_value)
                {
                    this.Startdate_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public DateTime Enddate
        {
            get { return this.Enddate_value; }
            set
            {
                if (value != this.Enddate_value)
                {
                    this.Enddate_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Interest
        {
            get { return this.Interest_value; }
            set
            {
                if (value != this.Interest_value)
                {
                    this.Interest_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Vs
        {
            get { return this.Vs_value; }
            set
            {
                if (value != this.Vs_value)
                {
                    this.Vs_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool Matchup
        {
            get { return this.Matchup_value; }
            set
            {
                if (value != this.Matchup_value)
                {
                    this.Matchup_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool Filter
        {
            get { return this.Filter_value; }
            set
            {
                if (value != this.Filter_value)
                {
                    this.Filter_value = value;
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
        public string Build
        {
            get { return this.Build_value; }
            set
            {
                if (value != this.Build_value)
                {
                    this.Build_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Gamemode
        {
            get { return this.Gamemode_value; }
            set
            {
                if (value != this.Gamemode_value)
                {
                    this.Gamemode_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string GenHash()
        {
            string opthash = "";
            foreach (var prop in this.GetType().GetProperties())
            {
                //Console.WriteLine("{0}={1}", prop.Name, prop.GetValue(this, null));
                if (prop.Name == "Update") continue;
                else if (prop.Name == "BeginAtZero") continue;
                else if (prop.Name == "OPT") continue;
                else if (prop.Name == "Icons") continue;
                else if (prop.Name == "DOIT") continue;
                else if (prop.Name == "Cmdrinfo") continue;
                else if (prop.Name == "Chart") continue;
                else if (prop.Name == "Total") continue;
                else if (prop.Name == "Ordered") continue;
                else if (prop.Name == "Hash") continue;
                else if (prop.Name == "CmdrsChecked") continue;

                if (prop.Name == "Enddate" && prop.GetValue(this, null).ToString() == DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"))
                {
                    opthash += prop.Name + "LaSt";
                }
                else if (prop.Name == "Gamemodes")
                {
                    Dictionary<string, bool> gm = prop.GetValue(this, null) as Dictionary<string, bool>;
                    foreach (var ent in gm.Keys)
                    {
                        opthash += gm[ent].ToString();
                    }
                }
                else if (prop.Name == "Players")
                {
                    Dictionary<string, bool> gm = prop.GetValue(this, null) as Dictionary<string, bool>;
                    foreach (var ent in gm.Keys)
                    {
                        opthash += gm[ent].ToString();
                    }
                }
                else
                {
                    opthash += prop.Name + prop.GetValue(this, null).ToString();
                }
            }
            //MD5 md5 = new MD5CryptoServiceProvider();
            //var plainTextBytes = Encoding.UTF8.GetBytes(opthash);
            //return BitConverter.ToString(md5.ComputeHash(plainTextBytes));
            return opthash;
        }

        public void DefaultFilter()
        {
            DSoptions defoptions = new DSoptions();
            this.DOIT = false;
            this.Build = "ALL";
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
            this.DOIT = true;
            this.Cmdrinfo["ALL"] = new CmdrInfo();
        }
    }
}
