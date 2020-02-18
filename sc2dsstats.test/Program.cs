using Microsoft.EntityFrameworkCore;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace sc2dsstats.test
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            //InsertData();
            //PrintData();

            //InitDB();
            //InitFilterDB();
            //PrintReps();
            //UpdateDB();
            //GetWR();
            //GetCmdrWR("Swann");
            //GetCmdrWR("Kerrigan");
            //GetCmdrWR("Swann");
            //GetMVP("Swann");
            GetSynergy("Swann");
        }

        private static void GetSynergy(string interest)
        {
            Dictionary<string, KeyValuePair<double, int>> winrate = new Dictionary<string, KeyValuePair<double, int>>();
            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                DSoptions opt = new DSoptions();
                opt.Startdate = new DateTime(2019, 1, 1);
                //opt.Enddate = new DateTime(2020, 1, 1);
                //opt.Interest = "Kerrigan";

                DateTime t1 = DateTime.UtcNow;
                //var replays = DBReplayFilter.Filter(opt, context, false);
                var replays = DBReplayFilter.Filter(opt, context, false);
                Console.WriteLine(replays.Count());
                Console.WriteLine((DateTime.UtcNow - t1).TotalSeconds);

                foreach (string cmdr in DSdata.s_races_cmdr)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    foreach (DSReplay rep in replays)
                        foreach (DSPlayer pl in rep.DSPlayer)
                        {
                            if (pl.RACE == interest)
                            {

                                foreach (DSPlayer tpl in rep.DSPlayer.Where(x => x.POS != pl.POS && x.RACE == cmdr && x.TEAM == pl.TEAM))
                                {
                                    games++;
                                    if (pl.TEAM == rep.WINNER)
                                        wins++;
                                }
                            }

                        }
                    wr = Math.Round(wins * 100 / games, 2);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));

                }
            }

            foreach (string cmdr in winrate.Keys)
            {
                Console.WriteLine($"{interest} vs {cmdr} => {winrate[cmdr].Key}% ({winrate[cmdr].Value})");
            }
        }

        private static void GetMVP(string interest)
        {
            Dictionary<string, KeyValuePair<double, int>> winrate = new Dictionary<string, KeyValuePair<double, int>>();
            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                DSoptions opt = new DSoptions();
                opt.Startdate = new DateTime(2019, 1, 1);
                //opt.Enddate = new DateTime(2020, 1, 1);
                //opt.Interest = "Kerrigan";

                DateTime t1 = DateTime.UtcNow;
                //var replays = DBReplayFilter.Filter(opt, context, false);
                var replays = DBReplayFilter.Filter(opt, context, false);
                Console.WriteLine(replays.Count());
                Console.WriteLine((DateTime.UtcNow - t1).TotalSeconds);

                foreach (string cmdr in DSdata.s_races_cmdr)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    foreach (DSReplay rep in replays)
                        foreach (DSPlayer pl in rep.DSPlayer)
                        {
                            if (pl.RACE == cmdr)
                            {
                                games++;
                                if (pl.KILLSUM == rep.MAXKILLSUM)
                                    wins++;
                            }

                        }
                    wr = Math.Round(wins * 100 / games, 2);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));

                }
            }

            foreach (string cmdr in winrate.Keys)
            {
                Console.WriteLine($"{interest} vs {cmdr} => {winrate[cmdr].Key}%");
            }
        }

        private static void UpdateDB()
        {
            int i = 0;
            foreach (string line in File.ReadAllLines("/data/data.json"))
            {
                dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                rep.Init();
                rep.GenHash();

                string gametime = rep.GAMETIME.ToString();
                int year = int.Parse(gametime.Substring(0, 4));
                int month = int.Parse(gametime.Substring(4, 2));
                int day = int.Parse(gametime.Substring(6, 2));
                int hour = int.Parse(gametime.Substring(8, 2));
                int min = int.Parse(gametime.Substring(10, 2));
                int sec = int.Parse(gametime.Substring(12, 2));
                DateTime t = new DateTime(year, month, day, hour, min, sec);
                using (var context = new DSReplayContext())
                {
                    var dbrep = context.DSReplays.Single(s => s.HASH == rep.HASH);
                    dbrep.GAMETIME = t;
                    context.SaveChanges();
                }
                i++;
                if (i % 100 == 0)
                    Console.WriteLine(i);
            }

        }

        private static void InitDB(int count = 0)
        {
            using (var context = new DSReplayContext())
            {
                context.Database.EnsureCreated();
            }
            int i = 0;

            foreach (string line in File.ReadAllLines("/data/data.json"))
            {
                dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                rep.Init();
                rep.GenHash();
                InsertReplay(rep);
                i++;
                if (i % 100 == 0)
                {
                    Console.WriteLine(i);
                    if (count > 0 && i > count)
                        break;
                }
            }

        }
        private static void InitFilterDB(int count = 0)
        {
            using (var context = new DSReplayContext())
            {
                context.Database.EnsureCreated();
            }
            int i = 0;
            DSoptions options = new DSoptions();
            foreach (string line in File.ReadAllLines("/data/data.json"))
            {
                dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                rep.Init();
                rep.GenHash();
                if (
                TimeSpan.FromSeconds(rep.DURATION / 22.4) > TimeSpan.FromMinutes(5) &&
                rep.MAXLEAVER < options.Leaver &&
                rep.MINARMY > options.Army &&
                rep.MININCOME > options.Income &&
                rep.MINKILLSUM > options.Kills &&
                rep.PLAYERCOUNT == 6)
                    InsertReplay(rep);
                i++;
                if (i % 100 == 0)
                {
                    Console.WriteLine(i);
                    if (count > 0 && i > count)
                        break;
                }
            }
        }

        private static void InsertReplay(dsreplay rep)
        {
            using (var context = new DSReplayContext())
            {
                DSReplay dbrep = new DSReplay();
                dbrep.REPLAY = rep.REPLAY;
                string gametime = rep.GAMETIME.ToString();
                int year = int.Parse(gametime.Substring(0, 4));
                int month = int.Parse(gametime.Substring(4, 2));
                int day = int.Parse(gametime.Substring(6, 2));
                int hour = int.Parse(gametime.Substring(8, 2));
                int min = int.Parse(gametime.Substring(10, 2));
                int sec = int.Parse(gametime.Substring(12, 2));
                DateTime gtime = new DateTime(year, month, day, hour, min, sec);
                dbrep.GAMETIME = gtime;
                dbrep.WINNER = rep.WINNER;
                dbrep.DURATION = TimeSpan.FromSeconds(rep.DURATION / 22.4);
                dbrep.MINKILLSUM = rep.MINKILLSUM;
                dbrep.MAXKILLSUM = rep.MAXKILLSUM;
                dbrep.MINARMY = rep.MINARMY;
                dbrep.MININCOME = rep.MININCOME;
                dbrep.MAXLEAVER = rep.MAXLEAVER;
                dbrep.PLAYERCOUNT = rep.PLAYERCOUNT;
                dbrep.REPORTED = rep.REPORTED;
                dbrep.ISBRAWL = rep.ISBRAWL.ToString();
                dbrep.GAMEMODE = rep.GAMEMODE;
                dbrep.VERSION = rep.VERSION;
                dbrep.HASH = rep.HASH;
                context.DSReplays.Add(dbrep);

                foreach (dsplayer pl in rep.PLAYERS)
                {
                    DSPlayer dbpl = new DSPlayer();
                    dbpl.POS = pl.POS;
                    dbpl.REALPOS = pl.REALPOS;
                    dbpl.NAME = pl.NAME;
                    dbpl.RACE = pl.RACE;
                    dbpl.RESULT = pl.RESULT;
                    dbpl.TEAM = pl.TEAM;
                    dbpl.KILLSUM = pl.KILLSUM;
                    dbpl.INCOME = pl.INCOME;
                    dbpl.PDURATION = TimeSpan.FromSeconds(pl.PDURATION / 22.4);
                    dbpl.ARMY = pl.ARMY;
                    dbpl.GAS = pl.GAS;
                    dbpl.DSReplay = dbrep;
                    context.DSPlayers.Add(dbpl);

                    foreach (var bp in pl.UNITS.Keys)
                        foreach (var name in pl.UNITS[bp].Keys)
                        {
                            DSUnit dbunit = new DSUnit();
                            dbunit.BP = bp;
                            dbunit.Name = name;
                            dbunit.Count = pl.UNITS[bp][name];
                            dbunit.DSPlayer = dbpl;
                            context.DSUnits.Add(dbunit);
                        }
                }

                foreach (var dup in rep.PLDupPos)
                {
                    PLDuplicate dbdup = new PLDuplicate();
                    dbdup.Hash = dup.Key;
                    dbdup.Pos = dup.Value;
                    dbdup.DSReplay = dbrep;
                    context.PLDuplicates.Add(dbdup);
                }

                context.SaveChanges();
            }
        }

        private static void GetWR()
        {
            Dictionary<string, KeyValuePair<double, int>> winrate = new Dictionary<string, KeyValuePair<double, int>>();
            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                DSoptions opt = new DSoptions();
                opt.Startdate = new DateTime(2019, 1, 1);
                //opt.Enddate = new DateTime(2020, 1, 1);
                //opt.Interest = "Kerrigan";

                DateTime t1 = DateTime.UtcNow;
                //var replays = DBReplayFilter.Filter(opt, context, false);
                var replays = DBReplayFilter.Filter(opt, context, false);
                Console.WriteLine(replays.Count());
                Console.WriteLine((DateTime.UtcNow - t1).TotalSeconds);

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    foreach (DSReplay rep in replays)
                        foreach (DSPlayer pl in rep.DSPlayer.Where(x => x.NAME == "player" && x.RACE == cmdr))
                        {
                            games++;
                            if (pl.TEAM == rep.WINNER)
                                wins++;
                        }

                    //games = replays.Where(x => x.DSPlayer.FirstOrDefault(s => s.NAME == "player") != null && x.DSPlayer.FirstOrDefault(s => s.NAME == "player").RACE == cmdr).Count();
                    //wins = replays.Where(x => x.DSPlayer.FirstOrDefault(s => s.NAME == "player") != null && x.DSPlayer.FirstOrDefault(s => s.NAME == "player").RACE == cmdr && x.DSPlayer.FirstOrDefault(s => s.NAME == "player").TEAM == x.WINNER).Count(); 

                    wr = Math.Round(wins * 100 / games, 2);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                }
                Console.WriteLine((DateTime.UtcNow - t1).TotalSeconds);
            }

            foreach (string cmdr in winrate.Keys)
            {
                Console.WriteLine(cmdr + " => " + winrate[cmdr].Key);
            }
        }

        private static void GetCmdrWR(string interest)
        {
            Dictionary<string, KeyValuePair<double, int>> winrate = new Dictionary<string, KeyValuePair<double, int>>();
            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                DSoptions opt = new DSoptions();
                opt.Startdate = new DateTime(2019, 1, 1);

                DateTime t1 = DateTime.UtcNow;
                //var replays = DBReplayFilter.Filter(opt, context, false);
                var replays = DBReplayFilter.Filter(opt, context, false);
                Console.WriteLine(replays.Count());
                Console.WriteLine((DateTime.UtcNow - t1).TotalSeconds);

                foreach (var cmdr in DSdata.s_races)
                {
                    double games = 0;
                    double wins = 0;
                    double wr = 0;

                    foreach (DSReplay rep in replays.Where(x => x.DSPlayer.FirstOrDefault(s => s.NAME == "player") != null && x.DSPlayer.FirstOrDefault(s => s.NAME == "player").RACE == interest))
                    {
                        DSPlayer pl = rep.DSPlayer.FirstOrDefault(s => s.NAME == "player");
                        int opppos = GetOpp(pl.REALPOS);
                        DSPlayer opp = rep.DSPlayer.FirstOrDefault(s => s.REALPOS == opppos);
                        if (opp != null && opp.RACE == cmdr)
                        {
                            games++;
                            if (pl.TEAM == rep.WINNER)
                                wins++;
                        }

                    }
                    wr = Math.Round(wins * 100 / games, 2);
                    winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                }
                Console.WriteLine((DateTime.UtcNow - t1).TotalSeconds);
            }

            foreach (string cmdr in winrate.Keys)
            {
                Console.WriteLine($"{interest} vs {cmdr} => {winrate[cmdr].Key}%");
            }
        }

        private static int GetOpp(int pos)
        {
            if (pos <= 3)
                return pos + 3;
            else
                return pos - 3;
        }

        private static void PrintReps()
        {
            using (var context = new DSReplayContext())
            {
                var replays = context.DSReplays
                    .Include(p => p.DSPlayer);


                foreach (var rep in replays.Where(x => x.DURATION > TimeSpan.FromHours(1)))
                //foreach (var rep in replays)
                {
                    var data = new StringBuilder();
                    data.AppendLine($"REPLAY: {rep.REPLAY}");
                    data.AppendLine($"PFDuration:  { rep.DSPlayer.FirstOrDefault().PDURATION.ToString()}");
                    Console.WriteLine(data.ToString());
                }
            }
        }


    }
}

