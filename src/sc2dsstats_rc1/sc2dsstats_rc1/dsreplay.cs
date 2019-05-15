using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace sc2dsstats_rc1
{
    public class dsreplay
    {
        public int ID { get; set; }
        public string REPLAY { get; set; }
        public double GAMETIME { get; set; }
        public int WINNER { get; set; }
        public int DURATION { get; set; }
        public List<dsplayer> PLAYERS { get; set; } = new List<dsplayer>();
        [JsonIgnore]
        public List<string> RACES { get; set; }
        public int MINKILLSUM { get; set; }
        public int MAXKILLSUM { get; set; }
        public int MINARMY { get; set; }
        public double MININCOME { get; set; }
        public int MAXLEAVER { get; set; }
        public int PLAYERCOUNT { get; set; }
        public string MMID { get; set; } = "0";
        public int REPORTED { get; set; } = 0;

        public dsreplay()
        {

        }

        public void Init()
        {
            RACES = _RACES();
            MINKILLSUM = _MINKILLSUM();
            MAXKILLSUM = _MAXKILLSUM();
            MINARMY = _MINARMY();
            MININCOME = _MININCOME();
            MAXLEAVER = _MAXLEAVER();
            PLAYERCOUNT = PLAYERS.Count;
        }

        public dsplayer GetOpp(int pos)
        {
            string opp = null;
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
            foreach (dsplayer pl in PLAYERS)
            {
                races.Add(pl.RACE);
            }
            return races;
        }

        public dsreplay(List<dsplayer> player)
        {
            PLAYERS = player;
        }



    }

    public class dsplayer
    {
        public int POS { get; set; }
        public int REALPOS { get; set; }
        public string NAME { get; set; }
        public string RACE { get; set; }
        public int RESULT { get; set; }
        public int TEAM { get; set; }
        public int KILLSUM { get; set; } = 0;
        public double INCOME { get; set; } = 0;
        public int PDURATION { get; set; } = 0;

        public int ARMY { get; set; } = 0;
        public Dictionary<string, Dictionary<string, int>> UNITS { get; set; } = new Dictionary<string, Dictionary<string, int>>();
        [JsonIgnore]
        public double ELO { get; set; }
        public double ELO_CHANGE { get; set; }

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

    class dsfilter
    {
        public int GAMES { get; set; }
        public int FILTERED { get; set; }
        public int Beta { get; set; }
        public int Hots { get; set; }
        public int Gametime { get; set; }
        public int Duration { get; set; }
        public int Leaver { get; set; }
        public int Killsum { get; set; }
        public int Army { get; set; }
        public int Income { get; set; }
        public int Std { get; set; }

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
                double filtered = FilterRate(this.FILTERED);
                bab = this.FILTERED + " / " + this.GAMES + " filtered (" + filtered.ToString() + "%)";
                bab += Environment.NewLine;
                double f = FilterRate(this.Beta);
                bab += "Beta: " + this.Beta + " (" + f + "%)" + "; ";
                f = FilterRate(this.Hots);
                bab += "Hots: " + this.Hots + " (" + f + "%)" + "; ";
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
                bab += "Std: " + this.Std + " (" + f + "%)" + "; ";
            }
            return bab;
        }

        public double FilterRate(int f)
        {
            double i = 0;
            double games = this.GAMES;
            if (games > 0)
            {
                i = f * 100 / games;
                i = Math.Round(i, 2);
            }
            return i;
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

            if (opp_race != null)
            {
                dsstats_vs cmdr_vs = new dsstats_vs();
                cmdr_vs = cmdr.OPP;
                cmdr_vs.GAMES++;
                dsstats_race cmdr_opp = new dsstats_race();
                cmdr_opp = cmdr_vs.objRaceVS(opp_race.RACE);
                cmdr_opp.RGAMES++;
                cmdr_opp.AddGame(race.PDURATION);
            } else
            {
                Console.WriteLine("no opp :(");
            }
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

            if (opp != null)
            {
                dsstats_vs cmdr_vs = new dsstats_vs();
                cmdr_vs = cmdr.OPP;
                cmdr_vs.GAMES++;
                dsstats_race cmdr_opp = new dsstats_race();
                cmdr_opp = cmdr_vs.objRaceVS(opp.RACE);
                cmdr_opp.RGAMES++;

                cmdr_opp.DPS += pl.GetDPS();
                cmdr_opp.DPM += pl.GetDPM();
                cmdr_opp.DPV += pl.GetDPV();
            } else
            {
                Console.WriteLine("no opp2 :(");
            }
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
}

