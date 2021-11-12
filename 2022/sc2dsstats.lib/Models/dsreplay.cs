using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace sc2dsstats.lib.Models
{
    [Serializable]
    class dsreplays
    {
        public List<dsreplay> REPLAYS { get; set; } = new List<dsreplay>();
    }

    [Serializable]
    public class dsreplay
    {
        public int ID { get; set; }
        public string REPLAY { get; set; }
        public double GAMETIME { get; set; }
        public int WINNER { get; set; } = -1;
        public int DURATION { get; set; } = 0;
        public List<dsplayer> PLAYERS { get; set; } = new List<dsplayer>();
        public List<string> RACES { get; set; }
        public int MINKILLSUM { get; set; }
        public int MAXKILLSUM { get; set; }
        public int MINARMY { get; set; }
        public double MININCOME { get; set; }
        public int MAXLEAVER { get; set; }
        public int PLAYERCOUNT { get; set; }
        public int REPORTED { get; set; } = 0;
        public bool ISBRAWL { get; set; } = false;
        public string GAMEMODE { get; set; } = "unknown";
        public string VERSION { get; set; } = "1.0";
        public Dictionary<string, int> PLDupPos { get; set; } = new Dictionary<string, int>();
        public string HASH { get; set; }
        [JsonIgnore]
        public List<KeyValuePair<int, int>> MIDDLE { get; set; } = new List<KeyValuePair<int, int>>();
        [JsonIgnore]
        public List<UnitEvent> UnitBorn { get; set; } = new List<UnitEvent>();
        [JsonIgnore]
        public Dictionary<int, Dictionary<int, UnitLife>> UnitLife { get; set; } = new Dictionary<int, Dictionary<int, UnitLife>>();
        [JsonIgnore]
        public Dictionary<int, List<int>> Spawns { get; set; } = new Dictionary<int, List<int>>();
        [JsonIgnore]
        public List<Refinery> Refineries { get; set; } = new List<Refinery>();
        [JsonIgnore]
        public int LastSpawn { get; set; } = 0;

        public dsreplay()
        {

        }

        public void Init()
        {
            MINKILLSUM = _MINKILLSUM();
            MAXKILLSUM = _MAXKILLSUM();
            MINARMY = _MINARMY();
            MININCOME = _MININCOME();
            MAXLEAVER = _MAXLEAVER();
            PLAYERCOUNT = PLAYERS.Count;

            FixPos();
            PLAYERS = PLAYERS.OrderBy(x => x.REALPOS).ToList();
            RACES = _RACES();
        }

        public string GenHash()
        {
            string md5 = "";
            string hashstring = "";
            foreach (dsplayer pl in PLAYERS.OrderBy(o => o.POS))
            {
                hashstring += pl.POS + pl.RACE;
            }
            hashstring += MINARMY + MINKILLSUM + MININCOME + MAXKILLSUM;
            using (MD5 md5Hash = MD5.Create())
            {
                md5 = GetMd5Hash(md5Hash, hashstring);
            }
            HASH = md5;
            return md5;
        }

        public void FixPos()
        {
            int DEBUG = 0;

            foreach (dsplayer pl in this.PLAYERS.OrderBy(o => o.POS))
            {
                if (pl.REALPOS == 0)
                {
                    for (int j = 1; j <= 6; j++)
                    {
                        if (this.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (this.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;

                        List<dsplayer> temp = new List<dsplayer>(this.PLAYERS.Where(x => x.REALPOS == j).ToList());
                        if (temp.Count == 0)
                        {
                            pl.REALPOS = j;
                            if (DEBUG > 0) Console.WriteLine("Fixing missing playerid for " + pl.POS + "|" + pl.REALPOS + " => " + j);
                        }
                    }



                    if (new List<dsplayer>(this.PLAYERS.Where(x => x.REALPOS == pl.POS).ToList()).Count == 0) pl.REALPOS = pl.POS;

                }

                if (new List<dsplayer>(this.PLAYERS.Where(x => x.REALPOS == pl.REALPOS).ToList()).Count > 1)
                {
                    if (DEBUG > 0) Console.WriteLine("Found double playerid for " + pl.POS + "|" + pl.REALPOS);

                    for (int j = 1; j <= 6; j++)
                    {
                        if (this.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (this.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;
                        if (new List<dsplayer>(this.PLAYERS.Where(x => x.REALPOS == j).ToList()).Count == 0)
                        {
                            pl.REALPOS = j;
                            if (DEBUG > 0) Console.WriteLine("Fixing double playerid for " + pl.POS + "|" + pl.REALPOS + " => " + j);
                            break;
                        }
                    }

                }

            }
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public dsplayer GetOpp(int pos)
        {
            dsplayer plopp = new dsplayer();
            if (this.PLAYERCOUNT == 6)
            {

                if (pos == 1) plopp = this.PLAYERS.Find(x => x.REALPOS == 4);
                if (pos == 2) plopp = this.PLAYERS.Find(x => x.REALPOS == 5);
                if (pos == 3) plopp = this.PLAYERS.Find(x => x.REALPOS == 6);
                if (pos == 4) plopp = this.PLAYERS.Find(x => x.REALPOS == 1);
                if (pos == 5) plopp = this.PLAYERS.Find(x => x.REALPOS == 2);
                if (pos == 6) plopp = this.PLAYERS.Find(x => x.REALPOS == 3);
                //opp = plopp.RACE;
            }
            else if (this.PLAYERCOUNT == 4)
            {
                if (pos == 1) plopp = this.PLAYERS.Find(x => x.REALPOS == 4);
                if (pos == 2) plopp = this.PLAYERS.Find(x => x.REALPOS == 5);
                if (pos == 4) plopp = this.PLAYERS.Find(x => x.REALPOS == 1);
                if (pos == 5) plopp = this.PLAYERS.Find(x => x.REALPOS == 2);
            }
            else if (this.PLAYERCOUNT == 2)
            {
                if (pos == 1) plopp = this.PLAYERS.Find(x => x.REALPOS == 4);
                if (pos == 4) plopp = this.PLAYERS.Find(x => x.REALPOS == 1);
            }
            if (plopp == null)
            {
                foreach (var ent in GetOpponents(this.PLAYERS.Find(x => x.REALPOS == pos)))
                {
                    plopp = ent;
                    break;
                }
            }
            if (plopp == null)
            {
                plopp = this.PLAYERS.Find(x => x.REALPOS == pos);
            }

            return plopp;
        }

        public List<dsplayer> GetTeammates(dsplayer pl)
        {
            List<dsplayer> teammates = new List<dsplayer>();

            foreach (dsplayer tm in PLAYERS)
            {
                if (pl.POS == tm.POS) continue;
                if (pl.TEAM == tm.TEAM) teammates.Add(tm);
            }

            return teammates;
        }

        public List<dsplayer> GetOpponents(dsplayer pl)
        {
            List<dsplayer> opponents = new List<dsplayer>();

            foreach (dsplayer tm in PLAYERS)
            {
                if (pl.POS == tm.POS) continue;
                if (pl.TEAM != tm.TEAM) opponents.Add(tm);
            }

            return opponents;
        }

        public int _MAXLEAVER()
        {
            int max = 0;
            foreach (dsplayer pl in PLAYERS)
            {
                int diff = DURATION - pl.PDURATION;
                if (diff > max) max = diff;
            }
            return max;
        }
        public int _MAXKILLSUM()
        {
            int max = 0;
            foreach (dsplayer pl in PLAYERS)
            {
                if (pl.KILLSUM > max) max = pl.KILLSUM;
            }
            return max;
        }

        public int _MINKILLSUM()
        {
            int min = -1;
            foreach (dsplayer pl in PLAYERS)
            {
                if (min == -1) min = pl.KILLSUM;
                else
                {
                    if (pl.KILLSUM < min) min = pl.KILLSUM;
                }
            }
            return min;
        }
        public int _MINARMY()
        {
            int min = -1;
            foreach (dsplayer pl in PLAYERS)
            {
                if (min == -1) min = pl.ARMY;
                else
                {
                    if (pl.ARMY < min) min = pl.ARMY;
                }
            }
            return min;
        }
        public double _MININCOME()
        {
            double min = -1;
            foreach (dsplayer pl in PLAYERS)
            {
                if (min == -1) min = pl.INCOME;
                else
                {
                    if (pl.INCOME < min) min = pl.INCOME;
                }
            }
            return min;
        }
        public List<string> _RACES()
        {
            List<string> races = new List<string>();
            foreach (dsplayer pl in PLAYERS.OrderBy(o => o.REALPOS))
            {
                races.Add(pl.RACE);
            }
            return races;
        }

        public dsreplay(List<dsplayer> player)
        {
            PLAYERS = player;
        }

        public string GetDuration()
        {
            double dur = 0;
            dur = DURATION / 22.4;
            TimeSpan t = TimeSpan.FromSeconds(dur);
            if (t.Hours > 0) return t.Hours + ":" + t.Minutes.ToString("D2") + ":" + t.Seconds.ToString("D2");
            return t.Minutes + ":" + t.Seconds.ToString("D2");
        }

        public double GetArmy(int team)
        {
            double army = 0;
            foreach (dsplayer pl in PLAYERS.Where(x => x.TEAM == team))
            {
                army += pl.ARMY;
            }
            return army / 1000;
        }

        public string GetArmyDiff()
        {
            double army0 = GetArmy(0);
            double diff = army0 - GetArmy(1);
            double per = diff / army0;
            return per.ToString("P", CultureInfo.InvariantCulture);
        }

        public double GetIncome(int team)
        {
            double income = 0;
            foreach (dsplayer pl in PLAYERS.Where(x => x.TEAM == team))
            {
                income += pl.INCOME;
            }
            return income / 1000;
        }

        public string GetIncomeDiff()
        {
            double army0 = GetIncome(0);
            double diff = army0 - GetIncome(1);
            double per = diff / army0;
            return per.ToString("P", CultureInfo.InvariantCulture);
        }

        public double GetKilled(int team)
        {
            double kills = 0;
            foreach (dsplayer pl in PLAYERS.Where(x => x.TEAM == team))
            {
                kills += pl.KILLSUM;
            }
            return kills / 1000;
        }

        public string GetKilledDiff()
        {
            double army0 = GetKilled(0);
            double diff = army0 - GetKilled(1);
            double per = diff / army0;
            return per.ToString("P", CultureInfo.InvariantCulture);
        }

    }
    [Serializable]
    public class dsplayer
    {
        public int POS { get; set; }
        public int REALPOS { get; set; } = 0;
        [JsonIgnore]
        public int WORKINGSLOT { get; set; }
        public string NAME { get; set; }
        public string RACE { get; set; }
        public int RESULT { get; set; }
        public int TEAM { get; set; }
        public int KILLSUM { get; set; } = 0;
        public double INCOME { get; set; } = 0;
        public int PDURATION { get; set; } = 0;
        public int ARMY { get; set; } = 0;
        public int GAS { get; set; } = 0;
        public Dictionary<string, Dictionary<string, int>> UNITS { get; set; } = new Dictionary<string, Dictionary<string, int>>();
        [JsonIgnore]
        public Dictionary<int, Dictionary<string, int>> SPAWNS { get; set; } = new Dictionary<int, Dictionary<string, int>>();
        [JsonIgnore]
        public Dictionary<int, M_stats> STATS { get; set; } = new Dictionary<int, M_stats>();
        [JsonIgnore]
        public double ELO { get; set; }
        [JsonIgnore]
        public double ELO_CHANGE { get; set; }
        [JsonIgnore]
        public int LastSpawn { get; set; } = 0;
        [JsonIgnore]
        public int Spawned { get; set; } = 0;
        [JsonIgnore]
        public int LastSpawnKills { get; set; } = 0;
        [JsonIgnore]
        public int LastSpawnArmy { get; set; } = 0;
        [JsonIgnore]
        public Dictionary<int, List<string>> Upgrades { get; set; } = new Dictionary<int, List<string>>();
        [JsonIgnore]
        public Dictionary<int, List<string>> AbilityUpgrades { get; set; } = new Dictionary<int, List<string>>();

        public dsplayer() { }

        public double GetDPS()
        {
            double dps = 0;
            if (this.KILLSUM != 0)
            {
                double d = this.PDURATION;
                double k = this.KILLSUM;

                dps = k / d;
            }
            return dps;
        }

        public double GetDPM()
        {
            double dps = 0;
            if (this.KILLSUM != 0)
            {
                double k = this.INCOME;
                double d = this.KILLSUM;
                dps = d / k;
            }
            return dps;
        }

        public double GetDPV()
        {
            double dps = 0;
            if (this.KILLSUM != 0)
            {
                double k = this.ARMY;
                double d = this.KILLSUM;
                dps = d / k;
            }
            return dps;
        }

    }

    class dscsv
    {
        public int ID { get; set; }
        public int PLAYERID { get; set; }
        public string REPLAY { get; set; }
        public string NAME { get; set; }
        public string RACE { get; set; }
        public int TEAM { get; set; }
        public int RESULT { get; set; }
        public double INCOME { get; set; }
        public int ARMY { get; set; }
        public int KILLSUM { get; set; }
        public int DURATION { get; set; }
        public double GAMETIME { get; set; }
    }

    public class dsfilter
    {
        public int GAMES { get; set; }
        public int GAMESf { get; set; }
        public int FILTERED { get; set; }
        public int Beta { get; set; }
        public int Hots { get; set; }
        public int Playercount { get; set; } = 0;
        public int Gamemodes { get; set; } = 0;
        public int Gametime { get; set; }
        public int Duration { get; set; }
        public int Leaver { get; set; }
        public int Killsum { get; set; }
        public int Army { get; set; }
        public int Income { get; set; }
        public int Std { get; set; }
        public int Total { get; set; }
        public double WR { get; set; }
        public TimeSpan Average_Duration { get; set; }
        public Dictionary<string, double> Cmdrs { get; set; }
        public Dictionary<string, double> Cmdrs_wins { get; set; }
        public Dictionary<string, FilHelper> CmdrInfo { get; set; } = new Dictionary<string, FilHelper>();

        public dsfilter()
        {
            GAMES = 0;
            FILTERED = 0;
            Beta = 0;
            Hots = 0;
            Gametime = 0;
            Duration = 0;
            Leaver = 0;
            Killsum = 0;
            Army = 0;
            Income = 0;
            Std = 0;
        }

        public string Info()
        {
            string bab = "Und es war Sommer";
            if (this.GAMES != 0)
            {


                /**
                bab = this.FILTERED + " / " + this.GAMES + " filtered (" + filtered.ToString() + "%)";
                bab += Environment.NewLine;
                double f = FilterRate(this.Beta);
                bab += "Beta: " + this.Beta + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Hots);
                bab += "Hots: " + this.Hots + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Gametime);
                bab += "Gametime: " + this.Gametime + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Duration);
                bab += "Duration: " + this.Duration + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Leaver);
                bab += "Leaver: " + this.Leaver + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Killsum);
                bab += "Killsum: " + this.Killsum + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Army);
                bab += "Army: " + this.Army + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Income);
                bab += "Income: " + this.Income + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Std);
                bab += "Std: " + this.Std + " (" + f + "%)" + Environment.NewLine;
                **/

                if (this.FILTERED == 0)
                {
                    FILTERED = Beta + Hots + Gametime + Duration + Leaver + Killsum + Army + Income + Std;
                }
                GAMESf = GAMES;
                double filtered = FilterRate(this.FILTERED);
                GAMESf = GAMES;
                bab = "Average game duratione: " + Average_Duration.Minutes + ":" + Average_Duration.Seconds.ToString("D2") + " min; ";
                bab += this.FILTERED + " / " + this.GAMES + " filtered (" + filtered.ToString() + "%)";
                bab += Environment.NewLine;
                double f = FilterRate(this.Beta);
                //bab += "Beta: " + this.Beta + " (" + f + "%)" + "; ";
                f = FilterRate(this.Hots);
                //bab += "Hots: " + this.Hots + " (" + f + "%)" + "; ";
                f = FilterRate(this.Playercount);
                bab += "Playercount: " + this.Playercount + " (" + f + "%)" + "; ";
                f = FilterRate(this.Gamemodes);
                bab += "Gamemodes: " + this.Gamemodes + " (" + f + "%)" + "; ";
                f = FilterRate(this.Gametime);
                bab += "Gametime: " + this.Gametime + " (" + f + "%)" + "; ";
                f = FilterRate(this.Duration);
                bab += "Duration: " + this.Duration + " (" + f + "%)" + "; ";
                f = FilterRate(this.Leaver);
                bab += "Leaver: " + this.Leaver + " (" + f + "%)" + "; ";
                f = FilterRate(this.Killsum);
                bab += "Killsum: " + this.Killsum + " (" + f + "%)" + "; ";
                f = FilterRate(this.Army);
                bab += "Army: " + this.Army + " (" + f + "%)" + "; ";
                f = FilterRate(this.Income);
                bab += "Income: " + this.Income + " (" + f + "%)" + "; ";
                f = FilterRate(this.Std);
                //bab += "Std: " + this.Std + " (" + f + "%)" + "; ";

            }
            return bab;
        }

        public double FilterRate(int f)
        {
            double i = 0;
            double games = this.GAMESf;
            if (games > 0)
            {
                i = f * 100 / games;
                i = Math.Round(i, 2);
            }
            GAMESf -= f;
            return i;
        }

    }

    public class FilHelper
    {
        public int Games { get; set; } = 0;
        public string Duration { get; set; } = "0 min";
        public string Wr { get; set; } = "50%";

        public FilHelper(int _Games, string _Duration, string _Wr)
        {
            Games = _Games;
            Duration = _Duration;
            Wr = _Wr;
        }
    }

    public class dsstats
    {
        public int GAMES { get; set; }
        public int WINS { get; set; }
        public double DURATION { get; set; }

        public List<dsstats_race> LRACE { get; set; }

        public dsstats()
        {
        }

        public dsstats(List<dsstats_race> cmdr)
        {
            this.LRACE = cmdr;
        }

        public virtual double GetWR()
        {
            double wr = 0;
            if (this.GAMES == 0)
            {
                return 0;
            }
            else
            {
                double wins = this.WINS;
                double games = this.GAMES;
                wr = wins * 100 / games;
                wr = Math.Round(wr, 2);

            }
            return wr;
        }

        public virtual double GetDURATION()
        {
            double wr = 0;
            if (this.GAMES == 0)
            {
                return 0;
            }
            else
            {
                double wins = this.DURATION;
                double games = this.GAMES;
                wr = wins / games;

                wr /= 22.4;
                wr = Math.Round(wr, 2);

            }
            return wr;
        }

        public void Init()
        {
            string[] s_races = new string[]
            {
                    "Abathur",
                     "Alarak",
                     "Artanis",
                     "Dehaka",
                     "Fenix",
                     "Horner",
                     "Karax",
                     "Kerrigan",
                     "Nova",
                     "Raynor",
                     "Stetmann",
                     "Stukov",
                     "Swann",
                     "Tychus",
                     "Vorazun",
                     "Zagara",
                     "Protoss",
                     "Terran",
                     "Zerg"
            };
            List<dsstats_race> list = new List<dsstats_race>();

            foreach (string r in s_races)
            {
                dsstats_race cmdr = new dsstats_race();
                dsstats_vs vs = new dsstats_vs();
                vs.Init();
                cmdr.RACE = r;
                cmdr.OPP = vs;
                list.Add(cmdr);

            }
            this.LRACE = list;
        }

        public virtual dsstats_race objRace(string cmdr)
        {
            dsstats_race race = new dsstats_race();
            race = this.LRACE.Find(x => x.RACE == cmdr);
            return race;
        }

        public virtual void AddGame(dsplayer race, dsplayer opp_race)
        {
            dsstats_race cmdr = new dsstats_race();
            cmdr = this.objRace(race.RACE);
            this.GAMES++;
            cmdr.RGAMES++;
            cmdr.AddGame(race.PDURATION);

            dsstats_vs cmdr_vs = new dsstats_vs();
            cmdr_vs = cmdr.OPP;
            cmdr_vs.GAMES++;
            dsstats_race cmdr_opp = new dsstats_race();
            cmdr_opp = cmdr_vs.objRaceVS(opp_race.RACE);
            cmdr_opp.RGAMES++;
            cmdr_opp.AddGame(race.PDURATION);
        }

        public virtual void AddGame(double dur)
        {
            this.DURATION += dur;
        }

        public virtual void AddWin(dsplayer race, dsplayer opp_race)
        {
            dsstats_race cmdr = new dsstats_race();
            cmdr = this.objRace(race.RACE);
            this.WINS++;
            cmdr.RWINS++;

            dsstats_vs cmdr_vs = new dsstats_vs();
            cmdr_vs = cmdr.OPP;
            cmdr_vs.WINS++;
            dsstats_race cmdr_opp = new dsstats_race();
            cmdr_opp = cmdr_vs.objRaceVS(opp_race.RACE);
            cmdr_opp.RWINS++;

        }
        public virtual void Sort()
        {
            try
            {
                this.LRACE.Sort(delegate (dsstats_race x, dsstats_race y)
                {
                    if (x.GetWR() == 0 && y.GetWR() == 0) return 0;
                    else if (x.GetWR() == 0) return -1;
                    else if (y.GetWR() == 0) return 1;
                    else return x.GetWR().CompareTo(y.GetWR());
                });
            }
            catch { }
        }

        public virtual void SortDPS()
        {
            try
            {
                this.LRACE.Sort(delegate (dsstats_race x, dsstats_race y)
                {
                    if (x.GetDPS() == 0 && y.GetDPS() == 0) return 0;
                    else if (x.GetDPS() == 0) return -1;
                    else if (y.GetDPS() == 0) return 1;
                    else return x.GetDPS().CompareTo(y.GetDPS());
                });
            }
            catch { }
        }
    }

    public class dsstats_race : dsstats
    {
        public string RACE { get; set; }
        public int RGAMES { get; set; }
        public int RWINS { get; set; }
        public dsstats_vs OPP { get; set; }
        public double DPS { get; set; }
        public double DPM { get; set; }
        public double DPV { get; set; }
        public double RDURATION { get; set; }

        public dsstats_race()
        {
        }

        public override double GetWR()
        {
            double wr = 0;
            if (this.RGAMES == 0)
            {
                return 0;
            }
            else
            {
                double wins = this.RWINS;
                double games = this.RGAMES;
                wr = wins * 100 / games;
                wr = Math.Round(wr, 2);

            }
            return wr;
        }

        public override void AddGame(double dur)
        {
            this.RDURATION += dur;
        }

        public override double GetDURATION()
        {
            double wr = 0;
            if (this.RGAMES == 0)
            {
                return 0;
            }
            else
            {
                double wins = this.RDURATION;
                double games = this.RGAMES;
                wr = wins / games;
                wr /= 22.4;
                wr = Math.Round(wr, 2);

            }
            return wr;
        }

        public double GetDPS()
        {
            double dps = 0;
            if (this.RGAMES > 0)
            {
                double games = this.RGAMES;
                dps = this.DPS / games;
                dps = Math.Round(dps, 2);
            }
            return dps;
        }

        public double GetDPM()
        {
            double dps = 0;
            if (this.RGAMES > 0)
            {
                double games = this.RGAMES;
                dps = this.DPM / games;
                dps = Math.Round(dps, 2);
            }
            return dps;
        }

        public double GetDPV()
        {
            double dps = 0;
            if (this.RGAMES > 0)
            {
                double games = this.RGAMES;
                dps = this.DPV / games;
                dps = Math.Round(dps, 2);
            }
            return dps;
        }

    }


    public class dsmvp : dsstats_race
    {
        public int MVP { get; set; }

        public dsmvp()
        {

        }
    }

    public class dsstats_vs
    {
        public string RACE { get; set; }
        public int GAMES { get; set; }
        public int WINS { get; set; }
        public List<dsstats_race> VS { get; set; }

        public dsstats_vs()
        {

        }

        public double GetWR()
        {
            double wr = 0;
            if (this.GAMES == 0)
            {
                return 0;
            }
            else
            {
                double wins = this.WINS;
                double games = this.GAMES;
                wr = wins * 100 / games;
                wr = Math.Round(wr, 2);

            }
            return wr;
        }

        public double GetDURATION(string race)
        {
            double dur = 0;

            dsstats_race cmdr = new dsstats_race();
            cmdr = VS.Find(x => x.RACE == race);
            dur = cmdr.GetDURATION();
            return dur;
        }

        public dsstats_race objRaceVS(string cmdr)
        {
            dsstats_race race = new dsstats_race();
            race = this.VS.Find(x => x.RACE == cmdr);
            return race;
        }

        public void Init()
        {
            string[] s_races = new string[]
            {
                    "Abathur",
                     "Alarak",
                     "Artanis",
                     "Dehaka",
                     "Fenix",
                     "Horner",
                     "Karax",
                     "Kerrigan",
                     "Nova",
                     "Raynor",
                     "Stetmann",
                     "Stukov",
                     "Swann",
                     "Tychus",
                     "Vorazun",
                     "Zagara",
                     "Protoss",
                     "Terran",
                     "Zerg"
            };
            List<dsstats_race> list = new List<dsstats_race>();

            foreach (string r in s_races)
            {
                dsstats_race cmdr = new dsstats_race();
                cmdr.RACE = r;
                list.Add(cmdr);

            }

            this.VS = list;
        }

        public virtual void Sort()
        {
            try
            {
                this.VS.Sort(delegate (dsstats_race x, dsstats_race y)
                {
                    if (x.GetWR() == 0 && y.GetWR() == 0) return 0;
                    else if (x.GetWR() == 0) return -1;
                    else if (y.GetWR() == 0) return 1;
                    else return x.GetWR().CompareTo(y.GetWR());
                });
            }
            catch { }
        }

    }

    class dsselect
    {
        public List<dsstats_race> LIST { get; set; }
        public List<KeyValuePair<string, double>> CLIST { get; set; }
        public int GAMES { get; set; }
        public double WINS { get; set; }
        public string TITLE { get; set; }
        public string YAXIS { get; set; }
        public int YMAX { get; set; }

    }

    class dsdps : dsstats_race
    {
        public dsdps()
        {

        }

        public override void AddGame(dsplayer pl, dsplayer opp)
        {
            dsstats_race cmdr = new dsstats_race();
            cmdr = this.objRace(pl.RACE);
            this.GAMES++;
            cmdr.RGAMES++;

            cmdr.DPS += pl.GetDPS();
            this.DPS += pl.GetDPS();

            cmdr.DPM += pl.GetDPM();
            this.DPM += pl.GetDPM();

            cmdr.DPV += pl.GetDPV();
            this.DPV += pl.GetDPV();


            dsstats_vs cmdr_vs = new dsstats_vs();
            cmdr_vs = cmdr.OPP;
            cmdr_vs.GAMES++;
            dsstats_race cmdr_opp = new dsstats_race();
            cmdr_opp = cmdr_vs.objRaceVS(opp.RACE);
            cmdr_opp.RGAMES++;

            cmdr_opp.DPS += pl.GetDPS();
            cmdr_opp.DPM += pl.GetDPM();
            cmdr_opp.DPV += pl.GetDPV();
        }

        public override void AddGame(double dur)
        {
            this.DURATION += dur;
        }

        public override void AddWin(dsplayer pl, dsplayer opp)
        {
            dsstats_race cmdr = new dsstats_race();
            cmdr = this.objRace(pl.RACE);
            this.WINS++;
            cmdr.RWINS++;

            dsstats_vs cmdr_vs = new dsstats_vs();
            cmdr_vs = cmdr.OPP;
            cmdr_vs.WINS++;
            dsstats_race cmdr_opp = new dsstats_race();
            cmdr_opp = cmdr_vs.objRaceVS(opp.RACE);
            cmdr_opp.RWINS++;

        }
    }

    [Serializable]
    public class M_stats
    {
        //public int FoodMade { get; set; } = 0;
        public int FoodUsed { get; set; } = 0;
        public int MineralsCollectionRate { get; set; } = 0;
        public int MineralsCurrent { get; set; } = 0;
        public int MineralsFriendlyFireArmy { get; set; } = 0;
        //public int MineralsFriendlyFireEconomy { get; set; } = 0;
        public int MineralsFriendlyFireTechnology { get; set; } = 0;
        public int MineralsKilledArmy { get; set; } = 0;
        //public int MineralsKilledEconomy { get; set; } = 0;
        public int MineralsKilledTechnology { get; set; } = 0;
        public int MineralsLostArmy { get; set; } = 0;
        //public int MineralsLostEconomy { get; set; } = 0;
        //public int MineralsLostTechnology { get; set; } = 0;
        public int MineralsUsedActiveForces { get; set; } = 0;
        public int MineralsUsedCurrentArmy { get; set; } = 0;
        //public int MineralsUsedCurrentEconomy { get; set; } = 0;
        public int MineralsUsedCurrentTechnology { get; set; } = 0;
        /**
        public int MineralsUsedInProgressArmy { get; set; } = 0;
        public int MineralsUsedInProgressEconomy { get; set; } = 0;
        public int MineralsUsedInProgressTechnology { get; set; } = 0;
        public int VespeneCollectionRate { get; set; } = 0;
        public int VespeneCurrent { get; set; } = 0;
        public int VespeneFriendlyFireArmy { get; set; } = 0;
        public int VespeneFriendlyFireEconomy { get; set; } = 0;
        public int VespeneFriendlyFireTechnology { get; set; } = 0;
        public int VespeneKilledArmy { get; set; } = 0;
        public int VespeneKilledEconomy { get; set; } = 0;
        public int VespeneKilledTechnology { get; set; } = 0;
        public int VespeneLostArmy { get; set; } = 0;
        public int VespeneLostEconomy { get; set; } = 0;
        public int VespeneLostTechnology { get; set; } = 0;
        public int VespeneUsedActiveForces { get; set; } = 0;
        public int VespeneUsedCurrentArmy { get; set; } = 0;
        public int VespeneUsedCurrentEconomy { get; set; } = 0;
        public int VespeneUsedCurrentTechnology { get; set; } = 0;
        public int VespeneUsedInProgressArmy { get; set; } = 0;
        public int VespeneUsedInProgressEconomy { get; set; } = 0;
        public int VespeneUsedInProgressTechnology { get; set; } = 0;
        public int WorkersActiveCount { get; set; } = 0;
        **/
        public double ArmyDiff { get; set; }
        public double KillsDiff { get; set; }
        public int Army { get; set; } = 0;
        public int Tier { get; set; } = 1;
        public int Gas { get; set; } = 0;
    }

    public class UnitEvent
    {
        public int Gameloop { get; set; }
        public int PlayerId { get; set; } = 0;
        public int KilledId { get; set; } = 0;
        public int KilledBy { get; set; }
        public int KillerRecycleTag { get; set; }
        public int Index { get; set; }
        public int RecycleTag { get; set; }
        public string Name { get; set; } = "";
        public int x { get; set; }
        public int y { get; set; }
        public int GameloopDied { get; set; }
        public int x_died { get; set; }
        public int y_died { get; set; }
    }

    public class UnitLife
    {
        public int Index { get; set; }
        public int RecycleTag { get; set; }
        public UnitEvent Born { get; set; }
        public UnitEvent Died { get; set; }
    }

    public class Refinery
    {
        public int Index { get; set; }
        public int RecycleTag { get; set; }
        public int PlayerId { get; set; }
        public bool Taken { get; set; } = false;
        public int Gameloop { get; set; } = 0;
    }
}

