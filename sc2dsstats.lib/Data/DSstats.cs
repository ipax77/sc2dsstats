using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace sc2dsstats.lib.Data
{
    /*
    public class DSstats
    {
        public Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winrate_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>> winratevs_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>();
        public Dictionary<string, dsfilter> filter_CACHE { get; private set; } = new Dictionary<string, dsfilter>();
        public Dictionary<string, KeyValuePair<CmdrInfo, CmdrInfo>> CmdrInfo_CACHE { get; private set; } = new Dictionary<string, KeyValuePair<CmdrInfo, CmdrInfo>>();

        public void GetDynData(DSoptions opt,
                        out Dictionary<string, KeyValuePair<double, int>> winrate,
                        out Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winratevs,
                        out string info
                        )
        { 
            string myhash = opt.GenHash();

            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            winratevs = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();
            info = "Computing.";
            if (opt.Interest == "")
            {
                if (winrate_CACHE.ContainsKey(myhash))
                {
                    winrate = winrate_CACHE[myhash];
                    if (CmdrInfo_CACHE.ContainsKey(myhash))
                        opt.Cmdrinfo["ALL"] = CmdrInfo_CACHE[myhash].Key;
                    info = "Winrate from Cache. " + myhash;
                    return;
                }
            }
            else
            {
                if (winratevs_CACHE.ContainsKey(myhash))
                {
                    winratevs = winratevs_CACHE[myhash];
                    if (CmdrInfo_CACHE.ContainsKey(myhash))
                    {
                        opt.Cmdrinfo["ALL"] = CmdrInfo_CACHE[myhash].Key;
                        opt.Cmdrinfo[opt.Interest] = CmdrInfo_CACHE[myhash].Value;
                    }
                    info = "WinrateVs from Cache." + myhash;
                    return;
                }
            }

            List<dsreplay> replays = new List<dsreplay>();
            DSfilter fil = new DSfilter();


            if (String.IsNullOrEmpty(opt.Dataset))
                (replays, fil) = DBfilter.Filter(DSdata.Replays, opt);
            else
            {
                (replays, fil) = DBfilter.Filter(DSdata.Replays.Where(x => x.PLDupPos.Keys.Contains(opt.Dataset)).ToList(), opt);
                foreach (dsreplay replay in replays.ToArray())
                {
                    int plpos = replay.PLAYERS.FindIndex(s => s.NAME == "player");
                    if (replay.PLDupPos.ContainsKey(opt.Dataset) && plpos != replay.PLDupPos[opt.Dataset])
                    {
                        string srep = JsonSerializer.Serialize(replay);
                        dsreplay rrep = JsonSerializer.Deserialize<dsreplay>(srep);
                        string rname = rrep.PLAYERS[replay.PLDupPos[opt.Dataset]].NAME;
                        rrep.PLAYERS[replay.PLDupPos[opt.Dataset]].NAME = "player";
                        rrep.PLAYERS[plpos].NAME = rname;
                        replays.Remove(replay);
                        replays.Add(rrep);
                    }
                }
            }

            if (opt.Mode == "Winrate")
            {
                if (opt.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.TEAM == x.WINNER)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.TEAM == rep.WINNER)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }
                        wr = GenWR(wins, games);
                        winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
                else if (opt.Interest != "")
                {
                    winratevs.Add(opt.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == cmdr && y.TEAM == x.WINNER)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == opt.Interest && rep.GetOpp(pl.REALPOS).RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.TEAM == rep.WINNER)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[opt.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            if (opt.Mode == "MVP")
            {
                if (opt.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.KILLSUM == x.MAXKILLSUM)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.KILLSUM == rep.MAXKILLSUM)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
                else if (opt.Interest != "")
                {
                    winratevs.Add(opt.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == cmdr && y.KILLSUM == x.MAXKILLSUM)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == opt.Interest && rep.GetOpp(pl.REALPOS).RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.KILLSUM == rep.MAXKILLSUM)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[opt.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            else if (opt.Mode == "Synergy")
            {
                if (opt.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    }
                }
                else if (opt.Interest != "")
                {
                    winratevs.Add(opt.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(pl.NAME) && pl.RACE == opt.Interest)
                                    {
                                        foreach (var ent in rep.GetTeammates(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == opt.Interest)
                                    {
                                        foreach (var ent in rep.GetTeammates(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[opt.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            else if (opt.Mode == "AntiSynergy")
            {
                if (opt.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    }
                }
                else if (opt.Interest != "")
                {
                    winratevs.Add(opt.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(pl.NAME) && pl.RACE == opt.Interest)
                                    {
                                        foreach (var ent in rep.GetOpponents(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == opt.Interest)
                                    {
                                        foreach (var ent in rep.GetOpponents(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[opt.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            else if (opt.Mode == "DPS")
            {
                if (opt.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(pl.NAME)) wins += pl.GetDPV();
                                }
                            }
                        }
                        else
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => y.RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                int i = -1;
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == cmdr)
                                    {
                                        wins += pl.GetDPV();
                                        i++;
                                    }
                                }
                                games += i;
                            }
                        }

                        wr = Math.Round(wins / games, 2);
                        winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
                else if (opt.Interest != "")
                {
                    winratevs.Add(opt.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(pl.NAME)) wins += pl.GetDPV();
                                }
                            }
                        }
                        else
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => y.RACE == opt.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                int i = -1;
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == opt.Interest && rep.GetOpp(pl.REALPOS).RACE == cmdr)
                                    {
                                        wins += pl.GetDPV();
                                        i++;
                                    }
                                }
                                games += i;
                            }
                        }

                        wr = Math.Round(wins / games, 2);
                        winratevs[opt.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }
            else if (opt.Mode == "Timeline")
            {
                if (opt.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races)
                    {
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    }
                }
                else
                {
                    DateTime startdate = opt.Startdate;
                    DateTime enddate = opt.Enddate;
                    DateTime breakpoint = startdate;
                    DSoptions topt = new DSoptions();
                    topt.Startdate = opt.Startdate;
                    topt.Enddate = opt.Enddate;
                    topt.Players = new Dictionary<string, bool>(opt.Players);
                    winratevs.Add(opt.Interest, new Dictionary<string, KeyValuePair<double, int>>());

                    while (DateTime.Compare(breakpoint, enddate) < 0)
                    {
                        breakpoint = breakpoint.AddDays(7);
                        topt.Enddate = breakpoint;
                        List<dsreplay> treplays = new List<dsreplay>();
                        (treplays, _) = DBfilter.Filter(replays.ToList(), topt);


                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = treplays.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.RACE == opt.Interest)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME) && y.TEAM == x.WINNER)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in treplays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == opt.Interest)
                                    {
                                        games++;
                                        if (pl.TEAM == rep.WINNER)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }
                        if (games >= 10)
                        {
                            wr = GenWR(wins, games);
                            winratevs[opt.Interest].Add(breakpoint.ToString("yyyy-MM-dd"), new KeyValuePair<double, int>(wr, (int)games));
                            startdate = breakpoint;
                            topt.Startdate = startdate;
                        }

                    }
                }
            }
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();
            (infoAll, infoCmdr) = GenCmdrInfo(replays, opt);
            infoAll.FilterInfo = fil.Info();

            winrate_CACHE[myhash] = winrate;
            winratevs_CACHE[myhash] = winratevs;
            CmdrInfo_CACHE[myhash] = new KeyValuePair<CmdrInfo, CmdrInfo>(infoAll, infoCmdr);
            opt.Cmdrinfo["ALL"] = infoAll;
            if (opt.Interest != "")
                opt.Cmdrinfo[opt.Interest] = infoCmdr;
        }

        public static double GenWr(int wins, int games)
        {
            double wr = 0;
            if (games > 0)
            {
                wr = (double)wins * 100 / (double)games;
                wr = Math.Round(wr, 2);
            }
            return wr;
        }

        public static double GenWR(double wins, double games)
        {
            double wr = 0;
            if (games > 0)
            {
                wr = wins * 100 / games;
                wr = Math.Round(wr, 2);
            }
            return wr;
        }

        (CmdrInfo, CmdrInfo) GenCmdrInfo(List<dsreplay> reps, DSoptions opt)
        {
            Dictionary<string, double> aduration = new Dictionary<string, double>();
            Dictionary<string, double> aduration_sum = new Dictionary<string, double>();
            Dictionary<string, double> cmdrs = new Dictionary<string, double>();
            Dictionary<string, double> cmdrs_wins = new Dictionary<string, double>();
            aduration.Add("ALL", 0);
            aduration_sum.Add("ALL", 0);
            double wins = 0;
            foreach (dsreplay rep in reps)
            {
                aduration["ALL"]++;
                aduration_sum["ALL"] += rep.DURATION;

                foreach (dsplayer pl in rep.PLAYERS)
                {
                    if (opt.Player == true && !opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(pl.NAME)) continue;
                    if (aduration.ContainsKey(pl.RACE)) aduration[pl.RACE]++;
                    else aduration.Add(pl.RACE, 1);
                    if (aduration_sum.ContainsKey(pl.RACE)) aduration_sum[pl.RACE] += rep.DURATION;
                    else aduration_sum.Add(pl.RACE, rep.DURATION);

                    if (cmdrs.ContainsKey(pl.RACE)) cmdrs[pl.RACE]++;
                    else cmdrs.Add(pl.RACE, 1);
                    if (pl.TEAM == rep.WINNER)
                    {
                        wins++;
                        if (cmdrs_wins.ContainsKey(pl.RACE)) cmdrs_wins[pl.RACE]++;
                        else cmdrs_wins.Add(pl.RACE, 1);
                    }
                }
            }
            double dur = 0;
            if (aduration["ALL"] > 0) dur = aduration_sum["ALL"] / aduration["ALL"];
            dur /= 22.4;

            CmdrInfo info = new CmdrInfo();
            info.Cmdr = "ALL";

            if (opt.Player == true)
                info.Games = reps.Where(x => x.PLAYERS.Exists(y => opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(y.NAME))).ToArray().Count();
            else
                info.Games = reps.Count();

            if (info.Games > 0 && opt.Player == true)
                info.Winrate = Math.Round(wins * 100 / (double)info.Games, 2).ToString();
            else
                info.Winrate = "50";

            TimeSpan t = TimeSpan.FromSeconds(dur);
            if (t.Hours > 0)
                info.AverageGameDuration = t.Hours + ":" + t.Minutes.ToString("D2") + ":" + t.Seconds.ToString("D2") + "min";
            else
                info.AverageGameDuration = t.Minutes + ":" + t.Seconds.ToString("D2") + "min";

            foreach (string cmdr in cmdrs.Keys)
                info.CmdrCount[cmdr] = (int)cmdrs[cmdr];

            CmdrInfo infoInt = new CmdrInfo();
            if (opt.Interest != "")
            {
                infoInt.Cmdr = opt.Interest;

                if (cmdrs.ContainsKey(opt.Interest))
                    infoInt.Games = (int)cmdrs[opt.Interest];
                else
                    infoInt.Games = 0;

                if (infoInt.Games > 0 && cmdrs_wins.ContainsKey(opt.Interest))
                    infoInt.Winrate = Math.Round(cmdrs_wins[opt.Interest] * 100 / (double)infoInt.Games, 2).ToString();
                else
                    infoInt.Winrate = "50";

                infoInt.AverageGameDuration = "";
                double mdur = 0;
                if (aduration.ContainsKey(opt.Interest) && aduration[opt.Interest] > 0 && aduration_sum.ContainsKey(opt.Interest))
                    mdur = aduration_sum[opt.Interest] / aduration[opt.Interest] / 22.4;

                t = TimeSpan.FromSeconds(mdur);
                if (t.Hours > 0)
                    infoInt.AverageGameDuration = t.Hours + ":" + t.Minutes.ToString("D2") + ":" + t.Seconds.ToString("D2") + "min";
                else
                    infoInt.AverageGameDuration = t.Minutes + ":" + t.Seconds.ToString("D2") + "min";
            }
            return (info, infoInt);
        }
    }
    */
}
