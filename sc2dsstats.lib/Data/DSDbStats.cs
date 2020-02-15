using System;
using System.Collections.Generic;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using System.Linq;
using System.Text;
using System.Text.Json;
using sc2dsstats.lib.Db;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace sc2dsstats.lib.Data
{
    public class DSDbStats
    {
        public Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winrate_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>> winratevs_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>();
        public Dictionary<string, dsfilter> filter_CACHE { get; private set; } = new Dictionary<string, dsfilter>();
        public Dictionary<string, CmdrInfo> CmdrInfo_CACHE { get; private set; } = new Dictionary<string, CmdrInfo>();


        public static (CmdrInfo, CmdrInfo) GetWinrate_maybe(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            CmdrInfo infoAll = new CmdrInfo();
            CmdrInfo infoCmdr = new CmdrInfo();

            DateTime t = DateTime.Now;
            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                Console.WriteLine((DateTime.Now - t).TotalSeconds);


                foreach (var cmdr in DSdata.s_races)
                {
                    DateTime tt = DateTime.Now;
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in context.DSPlayerResults
                                     where r.GAMETIME > opt.Startdate && r.GAMETIME < opt.Enddate
                                     where r.RACE == cmdr && r.NAME == "player"
                                     select new
                                     {
                                         r.TEAM,
                                         r.WINNER
                                     };
                        var resultlist = result.AsEnumerable();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.TEAM == x.WINNER).Count();

                    }
                    else if (String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in context.DSPlayerResults
                                     where r.GAMETIME > opt.Startdate && r.GAMETIME < opt.Enddate
                                     where r.RACE == cmdr
                                     select new
                                     {
                                         r.TEAM,
                                         r.WINNER
                                     };
                        var resultlist = result.AsEnumerable();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in context.DSPlayerResults
                                     where r.GAMETIME > opt.Startdate && r.GAMETIME < opt.Enddate
                                     where r.RACE == opt.Interest && r.NAME == "player" && r.OPPRACE == cmdr
                                     select new
                                     {
                                         r.TEAM,
                                         r.WINNER
                                     };
                        
                        var resultlist = result.AsEnumerable();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.TEAM == x.WINNER).Count();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in context.DSPlayerResults
                                     where r.GAMETIME > opt.Startdate && r.GAMETIME < opt.Enddate
                                     where r.RACE == opt.Interest && r.OPPRACE == cmdr
                                     select new
                                     {
                                         r.TEAM,
                                         r.WINNER
                                     };
                        var resultlist = result.AsEnumerable();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.TEAM == x.WINNER).Count();
                    }

                    wr = GenWR(wins, games);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));

                    Console.WriteLine(cmdr + " => " + (DateTime.Now - tt).TotalSeconds);
                }
            }
            Console.WriteLine("Total => " + (DateTime.Now - t).TotalSeconds);
            return (infoAll, infoCmdr);
        }


        public static void GetWinrate(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate, out CmdrInfo cmdrInfo)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            cmdrInfo = new CmdrInfo();
            HashSet<int> totalGameIDs = new HashSet<int>();

            DateTime t = DateTime.Now;
            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);

                Console.WriteLine((DateTime.Now - t).TotalSeconds);

                if (!String.IsNullOrEmpty(opt.Dataset))
                {
                    
                }

                foreach (var cmdr in DSdata.s_races)
                {
                    DateTime tt = DateTime.Now;
                    double games = 0;
                    double wins = 0;
                    double wr = 0;
                    TimeSpan duration = TimeSpan.Zero;
                    HashSet<int> gameIDs = new HashSet<int>();

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var resultlist = Task.Run(() =>
                        {
                            if (String.IsNullOrEmpty(opt.Dataset))
                            {
                                var result = from r in replays
                                             from t1 in r.DSPlayer
                                             where t1.PLDuplicate != null && t1.RACE == cmdr
                                             select new
                                             {
                                                 t1.WIN,
                                                 r.DURATION,
                                                 r.ID
                                             };
                                return result.ToList();
                            } else
                            {
                                var result = from r in replays
                                             from t1 in r.DSPlayer
                                             where t1.NAME == opt.Dataset && t1.RACE == cmdr
                                             select new
                                             {
                                                 t1.WIN,
                                                 r.DURATION,
                                                 r.ID
                                             };
                                return result.ToList();
                            }
                        }).GetAwaiter().GetResult();
                        
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.WIN == true).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();
                    }
                    else if (String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == cmdr
                                     select new
                                     {
                                         t1.WIN,
                                         r.DURATION,
                                         r.ID
                                     }
                                  ;
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.WIN == true).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                  from t1 in r.DSPlayer
                                  where t1.NAME=="player" && t1.RACE == opt.Interest && t1.OPPRACE == cmdr
                                  select new
                                  {
                                      t1.WIN,
                                      r.DURATION,
                                      r.ID
                                  }
                                  ;
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.WIN == true).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();

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
                                     where t1.RACE == opt.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.WIN,
                                         r.DURATION,
                                         r.ID
                                     }
                                  ;
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.WIN == true).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();
                    }


                    wr = GenWR(wins, games);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));

                    cmdrInfo.Games += games;
                    cmdrInfo.Wins += wins;
                    cmdrInfo.ADuration += duration;
                    cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));
                    totalGameIDs.UnionWith(gameIDs);

                    Console.WriteLine(cmdr + " => " + (DateTime.Now - tt).TotalSeconds);
                }
            }

            if (cmdrInfo.Games > 0)
                cmdrInfo.ADuration /= cmdrInfo.Games;
            cmdrInfo.AWinrate = GenWR(cmdrInfo.Wins, cmdrInfo.Games);
            cmdrInfo.Games = totalGameIDs.Count();

            Console.WriteLine("Total => " + (DateTime.Now - t).TotalSeconds);
        }

        
        public static void GetMVP(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate, out CmdrInfo cmdrInfo)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            cmdrInfo = new CmdrInfo();
            HashSet<int> totalGameIDs = new HashSet<int>();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;
                    TimeSpan duration = TimeSpan.Zero;
                    HashSet<int> gameIDs = new HashSet<int>();

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();

                    }
                    else if (String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == opt.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();

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
                                     where t1.RACE == opt.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         r.MAXKILLSUM,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();
                    }


                    wr = GenWR(wins, games);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));

                    cmdrInfo.Games += games;
                    cmdrInfo.Wins += wins;
                    cmdrInfo.ADuration += duration;
                    cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));
                    totalGameIDs.UnionWith(gameIDs);

                }
            }

            if (cmdrInfo.Games > 0)
                cmdrInfo.ADuration /= cmdrInfo.Games;
            cmdrInfo.AWinrate = GenWR(cmdrInfo.Wins, cmdrInfo.Games);
            cmdrInfo.Games = totalGameIDs.Count();
        }
        
        public static void GetDPS(DSoptions opt, out Dictionary<string, KeyValuePair<double, int>> winrate, out CmdrInfo cmdrInfo)
        {
            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            cmdrInfo = new CmdrInfo();
            HashSet<int> totalGameIDs = new HashSet<int>();

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(opt, context, false);

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;
                    TimeSpan duration = TimeSpan.Zero;
                    HashSet<int> gameIDs = new HashSet<int>();

                    if (String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();

                    }
                    else if (String.IsNullOrEmpty(opt.Interest))
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();
                    }
                    else if (!String.IsNullOrEmpty(opt.Interest) && opt.Player)
                    {
                        var result = from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME == "player" && t1.RACE == opt.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();

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
                                     where t1.RACE == opt.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.KILLSUM,
                                         t1.ARMY,
                                         r.DURATION,
                                         r.ID
                                     };
                        var resultlist = result.ToList();
                        games = resultlist.Count();
                        wins = resultlist.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
                        duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
                        gameIDs = resultlist.Select(s => s.ID).ToHashSet();
                    }


                    wr = Math.Round(wins / games, 2);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));

                    cmdrInfo.Games += games;
                    cmdrInfo.Wins += wins;
                    cmdrInfo.ADuration += duration;
                    cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));
                    totalGameIDs.UnionWith(gameIDs);

                }
            }

            if (cmdrInfo.Games > 0)
                cmdrInfo.ADuration /= cmdrInfo.Games;
            cmdrInfo.AWinrate = Math.Round(cmdrInfo.Wins /cmdrInfo.Games, 2);
            cmdrInfo.Games = totalGameIDs.Count();
        }
        /*
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
        */
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
                        opt.Cmdrinfo = CmdrInfo_CACHE[myhash];
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
                        opt.Cmdrinfo = CmdrInfo_CACHE[myhash];
                    info = "WinrateVs from Cache." + myhash;
                    return;
                }
            }
            CmdrInfo infoAll = new CmdrInfo();

            if (opt.Mode == "Winrate")
                GetWinrate(opt, out winrate, out infoAll);
            
            else if (opt.Mode == "MVP")
                GetMVP(opt, out winrate, out infoAll);
            
            else if (opt.Mode == "DPS")
                GetDPS(opt, out winrate, out infoAll);
            /*
            else if (opt.Mode == "Timeline")
                (infoAll, infoCmdr) = GetTimeline(opt, out winrate);
            else if (opt.Mode == "Synergy")
                (infoAll, infoCmdr) = GetSynergy(opt, out winrate);
            else if (opt.Mode == "AntiSynergy")
                (infoAll, infoCmdr) = GetAntiSynergy(opt, out winrate);
            */
            if (!String.IsNullOrEmpty(opt.Interest))
            {
                winratevs[opt.Interest] = new Dictionary<string, KeyValuePair<double, int>>();
                foreach (var ent in winrate)
                    winratevs[opt.Interest][ent.Key] = ent.Value;
            }
            else
            {
                //infoAll.Games = (int)(infoAll.Games / 6);
            }
 
            winrate_CACHE[myhash] = winrate;
            winratevs_CACHE[myhash] = winratevs;
            CmdrInfo_CACHE[myhash] = infoAll;
            opt.Cmdrinfo = infoAll;
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

    }
}
