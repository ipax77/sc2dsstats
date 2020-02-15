using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sc2dsstats.lib.Data
{
    public static class DBfilter
    {
        public static (List<dsreplay>, DSfilter) Filter(List<dsreplay> replays, string startdate = null, string enddate = null)
        {
            DSfilter FIL = new DSfilter();
            List<dsreplay> fil_replays = new List<dsreplay>(replays);
            List<dsreplay> tmprep = new List<dsreplay>();
            FIL.GAMES = replays.Count;

            if (true)
            {
                FIL.Beta = replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => !x.REPLAY.Contains("Beta")).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Beta -= fil_replays.Count;
            }

            if (true)
            {
                FIL.Hots = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => !x.REPLAY.Contains("HotS")).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Hots -= fil_replays.Count;
            }

            if (startdate != null && enddate != null)
            {
                // 20190323015855
                // 20190101000000
                string sd = startdate;
                sd += "000000";
                double sd_int = double.Parse(sd);
                string ed = enddate;
                ed += "999999";
                double ed_int = double.Parse(ed);

                FIL.Gametime = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => (x.GAMETIME > sd_int)).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                tmprep = new List<dsreplay>(fil_replays.Where(x => (x.GAMETIME < ed_int)).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Gametime -= fil_replays.Count;
            }

            if (true)
            {
                string duration = "5376";
                string mod = duration.Substring(0, 1);
                string snum = duration.Substring(1, duration.Length - 1);
                double num = 0;
                FIL.Duration = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(duration);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Duration -= tmprep.Count;
            }

            if (true)
            {
                string leaver = "2000";
                string mod = leaver.Substring(0, 1);
                string snum = leaver.Substring(1, leaver.Length - 1);
                double num = 0;
                FIL.Leaver = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(leaver);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Leaver -= tmprep.Count;
            }

            if (true)
            {
                string army = "1500";
                string mod = army.Substring(0, 1);
                string snum = army.Substring(1, army.Length - 1);
                double num = 0;
                FIL.Army = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.ARMY > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.ARMY < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(army);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MINARMY > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Army -= tmprep.Count;
            }

            if (true)
            {
                string income = "1500";
                string mod = income.Substring(0, 1);
                string snum = income.Substring(1, income.Length - 1);
                double num = 0;
                FIL.Income = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.INCOME > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.INCOME < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(income);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MININCOME > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Income -= tmprep.Count;
            }

            if (true)
            {
                string killsum = "1500";
                string mod = killsum.Substring(0, 1);
                string snum = killsum.Substring(1, killsum.Length - 1);
                double num = 0;
                FIL.Killsum = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.KILLSUM > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.KILLSUM < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(killsum);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MINKILLSUM > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Killsum -= tmprep.Count;
            }

            int count = 0;
            double aduration = 0;
            Dictionary<string, double> cmdrs = new Dictionary<string, double>();
            foreach (dsreplay rep in fil_replays)
            {
                if (rep.PLAYERCOUNT != 6) continue;
                count++;
                aduration += rep.DURATION;
                foreach (dsplayer pl in rep.PLAYERS)
                {
                    if (cmdrs.ContainsKey(pl.RACE)) cmdrs[pl.RACE]++;
                    else cmdrs.Add(pl.RACE, 1);
                }
            }
            double dur = 0;
            if (count > 0) dur = aduration / count;
            dur /= 22.4;

            TimeSpan t = TimeSpan.FromSeconds(dur);
            FIL.Average_Duration = t;
            FIL.Cmdrs = new Dictionary<string, double>(cmdrs);

            //fil_replays = fil_replays.Distinct().ToList();
            return (fil_replays, FIL);
        }
    

    public static (List<dsreplay>, DSfilter) Filter(List<dsreplay> replays, DSoptions opt)
        {
            DSfilter FIL = new DSfilter();
            string startdate = "";
            string enddate = "";
            try
            {
                startdate = opt.Startdate.ToString("yyyyMMdd");
                enddate = opt.Enddate.ToString("yyyyMMdd");
            }
            catch { }

            HashSet<string> Gamemodes = opt.Gamemodes.Where(x => x.Value == true).Select(y => y.Key).ToHashSet();


            List<dsreplay> fil_replays = new List<dsreplay>(replays);
            List<dsreplay> tmprep = new List<dsreplay>();
            FIL.GAMES = replays.Count;

            if (true)
            {
                FIL.Beta = replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => !x.REPLAY.Contains("Beta")).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Beta -= fil_replays.Count;
            }
            if (true)
            {
                FIL.Hots = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => !x.REPLAY.Contains("HotS")).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Hots -= fil_replays.Count;
            }

            if (opt.PlayerCount > 0)
            {
                FIL.Playercount = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERCOUNT == opt.PlayerCount).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Playercount -= fil_replays.Count;
            }

            FIL.Gamemodes = fil_replays.Count;
            tmprep = new List<dsreplay>(fil_replays.Where(x => Gamemodes.Contains(x.GAMEMODE))).ToList();
            fil_replays = new List<dsreplay>(tmprep);
            FIL.Gamemodes -= fil_replays.Count;

            if (startdate != null && enddate != null)
            {
                // 20190323015855
                // 20190101000000
                string sd = startdate;
                sd += "000000";
                double sd_int = double.Parse(sd);
                string ed = enddate;
                ed += "999999";
                double ed_int = double.Parse(ed);

                FIL.Gametime = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => (x.GAMETIME > sd_int)).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                tmprep = new List<dsreplay>(fil_replays.Where(x => (x.GAMETIME < ed_int)).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Gametime -= fil_replays.Count;
            }

            if (opt.Duration > TimeSpan.Zero)
            {
                string duration = opt.Duration.ToString();
                string mod = duration.Substring(0, 1);
                string snum = duration.Substring(1, duration.Length - 1);
                double num = 0;
                FIL.Duration = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(duration);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Duration -= tmprep.Count;
            }

            if (opt.Leaver > 0)
            {
                string leaver = opt.Leaver.ToString();
                string mod = leaver.Substring(0, 1);
                string snum = leaver.Substring(1, leaver.Length - 1);
                double num = 0;
                FIL.Leaver = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(leaver);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Leaver -= tmprep.Count;
            }

            if (opt.Army > 0)
            {
                string army = opt.Army.ToString();
                string mod = army.Substring(0, 1);
                string snum = army.Substring(1, army.Length - 1);
                double num = 0;
                FIL.Army = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.ARMY > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.ARMY < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(army);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MINARMY > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Army -= tmprep.Count;
            }

            if (opt.Income > 0)
            {
                string income = opt.Income.ToString();
                string mod = income.Substring(0, 1);
                string snum = income.Substring(1, income.Length - 1);
                double num = 0;
                FIL.Income = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.INCOME > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.INCOME < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(income);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MININCOME > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Income -= tmprep.Count;
            }

            if (opt.Kills > 0)
            {
                string killsum = opt.Kills.ToString();
                string mod = killsum.Substring(0, 1);
                string snum = killsum.Substring(1, killsum.Length - 1);
                double num = 0;
                FIL.Killsum = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.KILLSUM > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.KILLSUM < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(killsum);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MINKILLSUM > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Killsum -= tmprep.Count;
            }

            if (opt.Interest != null)
            {
                if (Data.DSdata.s_races.Contains(opt.Interest))
                {
                    if (opt.Player == false)
                    {
                        tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.RACE == opt.Interest)).ToList());
                    }
                    else
                    {
                        tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest)).ToList());
                    }
                    fil_replays = new List<dsreplay>(tmprep);
                }
            }

            if (opt.Vs != null && opt.Interest != null)
            {
                if (Data.DSdata.s_races.Contains(opt.Vs) && Data.DSdata.s_races.Contains(opt.Interest))
                {
                    if (opt.Player == false)
                    {
                        tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == opt.Vs)).ToList());
                    }
                    else
                    {
                        tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == opt.Vs)).ToList());
                    }
                    fil_replays = new List<dsreplay>(tmprep);

                }
            }

            return (fil_replays, FIL);
        }
    }
}
