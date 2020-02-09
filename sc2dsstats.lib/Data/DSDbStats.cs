using System;
using System.Collections.Generic;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using System.Linq;
using System.Text;
using System.Text.Json;
using sc2dsstats.lib.Db;
using Microsoft.EntityFrameworkCore;

namespace sc2dsstats.lib.Data
{
    public class DSDbStats
    {
        public Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winrate_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>> winratevs_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>();
        public Dictionary<string, dsfilter> filter_CACHE { get; private set; } = new Dictionary<string, dsfilter>();
        public Dictionary<string, KeyValuePair<CmdrInfo, CmdrInfo>> CmdrInfo_CACHE { get; private set; } = new Dictionary<string, KeyValuePair<CmdrInfo, CmdrInfo>>();


        public static (CmdrInfo, CmdrInfo) GetWinrate(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);
                (infoAll, infoCmdr) = GenCmdrInfo(replays, opt);
                infoAll.FilterInfo = "";

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == cmdr
                                     select new
                                     {
                                         t1.TEAM,
                                         r.WINNER
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();

                    }
                    else if (String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == cmdr
                                     select new
                                     {
                                         t1.TEAM,
                                         r.WINNER
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                  from t1 in r.DSPlayer
                                  where t1.NAME=="player" && t1.RACE == opt.Interest
                                  //join t2 in context.DSPlayers on t1.DSReplay equals t2.DSReplay into rep
                                  from t3 in r.DSPlayer
                                  where t3.REALPOS == DBFunctions.GetOpp(t1.REALPOS) && t3.RACE == cmdr
                                  select new
                                  {
                                      t1.TEAM,
                                      r.WINNER
                                  }
                                  ;
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();

                        /*
                        select t1.* from DSPlayers AS t1
                         left join DSPlayers as t2
                            on t1.DSReplayID = t2.DSReplayID
                         left join DSPlayers as t3
                            on t1.DSReplayID = t3.DSReplayID and t3.REALPOS = GetOpp(t2.REALPOS)
                            where t2.RACE = "Kerrigan" and t2.NAME="player"
                            and t3.RACE = "Swann" limit 20;

                        */
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == opt.Interest
                                     from t2 in r.DSPlayer
                                     where t2.REALPOS == DBFunctions.GetOpp(t1.REALPOS) && t2.RACE == cmdr
                                     select new
                                     {
                                         t1.TEAM,
                                         r.WINNER
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    wr = GenWR(wins, games);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                }
            }
            return (infoAll, infoCmdr);
        }

        public static (CmdrInfo, CmdrInfo) GetMVP(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);
                (infoAll, infoCmdr) = GenCmdrInfo(replays, opt);
                infoAll.FilterInfo = "";

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();

                    }
                    else if (String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == opt.Interest
                                     //join t2 in context.DSPlayers on t1.DSReplay equals t2.DSReplay into rep
                                     from t3 in r.DSPlayer
                                     where t3.REALPOS == DBFunctions.GetOpp(t1.REALPOS) && t3.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM
                                     };

                        games = result.Count();
                        wins = result.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == opt.Interest
                                     from t2 in r.DSPlayer
                                     where t2.REALPOS == DBFunctions.GetOpp(t1.REALPOS) && t2.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
                    }
                    wr = GenWR(wins, games);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                }
            }
            return (infoAll, infoCmdr);
        }

        public static (CmdrInfo, CmdrInfo) GetDPS(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);
                (infoAll, infoCmdr) = GenCmdrInfo(replays, opt);
                infoAll.FilterInfo = "";

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY
                                     };
                        games = result.Count();
                        wins = result.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                    }
                    else if (String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY
                                     };
                        games = result.Count();
                        wins = result.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == opt.Interest
                                     from t2 in r.DSPlayer
                                     where t2.REALPOS == DBFunctions.GetOpp(t1.REALPOS) && t2.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY
                                     };
                        games = result.Count();
                        wins = result.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == opt.Interest
                                     from t2 in r.DSPlayer
                                     where t2.REALPOS == DBFunctions.GetOpp(t1.REALPOS) && t2.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY
                                     };
                        games = result.Count();
                        wins = result.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                    }
                    wr = Math.Round(wins / games, 2);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));

                }
            }
            return (infoAll, infoCmdr);
        }

        public static (CmdrInfo, CmdrInfo) GetTimeline(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);

                (infoAll, infoCmdr) = GenCmdrInfo(replays, opt);
                infoAll.FilterInfo = "";


                if (!String.IsNullOrEmpty(opt.Interest))
                {
                    DateTime startdate = opt.Startdate;
                    DateTime enddate = opt.Enddate;
                    DateTime breakpoint = startdate;
                    DSoptions topt = new DSoptions();
                    topt.Startdate = opt.Startdate;
                    topt.Enddate = opt.Enddate;
                    topt.Players = new Dictionary<string, bool>(opt.Players);

                    while (DateTime.Compare(breakpoint, enddate) < 0)
                    {
                        breakpoint = breakpoint.AddDays(7);
                        topt.Enddate = breakpoint;

                        var treplays = DBReplayFilter.Filter(topt, context, true);

                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (opt.Player == true)
                        {
                            var result = from r in treplays
                                         from t1 in r.DSPlayer
                                         where t1.NAME == "player" && t1.RACE == opt.Interest
                                         select new
                                         {
                                             t1.TEAM,
                                             r.WINNER
                                         };
                            games = result.Count();
                            wins = result.Where(x => x.TEAM == x.WINNER).Count();
                        }
                        else
                        {
                            var result = from r in treplays
                                         from t1 in r.DSPlayer
                                         where t1.RACE == opt.Interest
                                         select new
                                         {
                                             t1.TEAM,
                                             r.WINNER
                                         };
                            games = result.Count();
                            wins = result.Where(x => x.TEAM == x.WINNER).Count();
                        }
                        if (games >= 10)
                        {
                            wr = GenWR(wins, games);
                            winrate[breakpoint.ToString("yyyy-MM-dd")] = new KeyValuePair<double, int>(wr, (int)games);
                            startdate = breakpoint;
                            topt.Startdate = startdate;
                        }

                    }

                }
            }
            return (infoAll, infoCmdr);
        }

        public static (CmdrInfo, CmdrInfo) GetSynergy(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);
                (infoAll, infoCmdr) = GenCmdrInfo(replays, opt);
                infoAll.FilterInfo = "";

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == opt.Interest
                                     from t3 in r.DSPlayer
                                     where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                                     select new
                                     {
                                         t1.TEAM,
                                         r.WINNER
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == opt.Interest
                                     from t3 in r.DSPlayer
                                     where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                                     select new
                                     {
                                         t1.TEAM,
                                         r.WINNER
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    wr = GenWR(wins, games);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                }
            }
            return (infoAll, infoCmdr);
        }

        public static (CmdrInfo, CmdrInfo) GetAntiSynergy(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);
                (infoAll, infoCmdr) = GenCmdrInfo(replays, opt);
                infoAll.FilterInfo = "";

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == opt.Interest
                                     from t3 in r.DSPlayer
                                     where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                                     select new
                                     {
                                         t1.TEAM,
                                         r.WINNER
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == opt.Interest
                                     from t3 in r.DSPlayer
                                     where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                                     select new
                                     {
                                         t1.TEAM,
                                         r.WINNER
                                     };
                        games = result.Count();
                        wins = result.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    wr = GenWR(wins, games);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                }
            }
            return (infoAll, infoCmdr);
        }
        
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

            DSfilter fil = new DSfilter();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            if (opt.Mode == "Winrate")
                (infoAll, infoCmdr) = GetWinrate(opt, out winrate);
            else if (opt.Mode == "MVP")
                (infoAll, infoCmdr) = GetMVP(opt, out winrate);
            else if (opt.Mode == "DPS")
                (infoAll, infoCmdr) = GetDPS(opt, out winrate);
            else if (opt.Mode == "Timeline")
                (infoAll, infoCmdr) = GetTimeline(opt, out winrate);
            else if (opt.Mode == "Synergy")
                (infoAll, infoCmdr) = GetSynergy(opt, out winrate);
            else if (opt.Mode == "AntiSynergy")
                (infoAll, infoCmdr) = GetAntiSynergy(opt, out winrate);

            if (!String.IsNullOrEmpty(opt.Interest)) {
                winratevs[opt.Interest] = new Dictionary<string, KeyValuePair<double, int>>();
                foreach (var ent in winrate)
                    winratevs[opt.Interest][ent.Key] = ent.Value;
            }
 
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

        private static int GetOpp(int pos)
        {
            if (pos <= 3)
                return pos + 3;
            else
                return pos - 3;
        }

        private static (CmdrInfo, CmdrInfo) GenCmdrInfo(IQueryable<DSReplay> reps, DSoptions opt)
        {
            Dictionary<string, double> aduration = new Dictionary<string, double>();
            Dictionary<string, TimeSpan> aduration_sum = new Dictionary<string, TimeSpan>();
            Dictionary<string, double> cmdrs = new Dictionary<string, double>();
            Dictionary<string, double> cmdrs_wins = new Dictionary<string, double>();
            aduration.Add("ALL", 0);
            aduration_sum.Add("ALL", TimeSpan.Zero);
            double wins = 0;
            foreach (DSReplay rep in reps)
            {
                aduration["ALL"]++;
                aduration_sum["ALL"] += rep.DURATION;

                foreach (DSPlayer pl in rep.DSPlayer)
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
            TimeSpan dur = TimeSpan.Zero;
            if (aduration["ALL"] > 0) dur = aduration_sum["ALL"] / aduration["ALL"];

            CmdrInfo info = new CmdrInfo();
            info.Cmdr = "ALL";

            if (opt.Player)
                info.Games = reps.Where(x => x.DSPlayer.FirstOrDefault(s => s.NAME == "player") != null).Count();
            else
                info.Games = reps.Count();

            if (info.Games > 0 && opt.Player == true)
                info.Winrate = Math.Round(wins * 100 / (double)info.Games, 2).ToString();
            else
                info.Winrate = "50";

            if (dur.Hours > 0)
                info.AverageGameDuration = dur.Hours + ":" + dur.Minutes.ToString("D2") + ":" + dur.Seconds.ToString("D2") + "min";
            else
                info.AverageGameDuration = dur.Minutes + ":" + dur.Seconds.ToString("D2") + "min";

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
                TimeSpan mdur = TimeSpan.Zero;
                if (aduration.ContainsKey(opt.Interest) && aduration[opt.Interest] > 0 && aduration_sum.ContainsKey(opt.Interest))
                    mdur = aduration_sum[opt.Interest] / aduration[opt.Interest];

                if (mdur.Hours > 0)
                    infoInt.AverageGameDuration = mdur.Hours + ":" + mdur.Minutes.ToString("D2") + ":" + mdur.Seconds.ToString("D2") + "min";
                else
                    infoInt.AverageGameDuration = mdur.Minutes + ":" + mdur.Seconds.ToString("D2") + "min";
            }
            return (info, infoInt);
        }
    }
}
