using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.lib.Data
{
    public class DSbuilds
    {
        static Regex rx_star = new Regex(@"(.*)Starlight(.*)", RegexOptions.Singleline);
        static Regex rx_light = new Regex(@"(.*)Lightweight(.*)", RegexOptions.Singleline);
        static Regex rx_hero = new Regex(@"Hero(.*)WaveUnit", RegexOptions.Singleline);
        static Regex rx_mp = new Regex(@"(.*)MP$", RegexOptions.Singleline);

        // build, cmdr, cmdr_vs, breakpoint, unit, count | wr, games
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>> BUILDCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>> BUILDWRCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>> BUILDDURCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>();
        // build, cmdr, vs, replays
        public Dictionary<string, Dictionary<string, Dictionary<string, List<dsreplay>>>> BUILDREPLAYSCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, List<dsreplay>>>>();

        public Dictionary<string, List<dsreplay>> BUILD_REPLAYS { get; private set; } = new Dictionary<string, List<dsreplay>>();
        // mode, startdate, enddate, filter
        public Dictionary<string, Dictionary<string, Dictionary<string, DSfilter>>> FILTER { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, DSfilter>>>();
        private bool BuildUpdateNeeded = true;

        public DSbuilds()
        {

        }

        public async Task Init(List<dsreplay> replays)
        {
            await Task.Run(() =>
            {
                DSdata.Enddate = DateTime.Today.AddDays(1).ToString("yyyyMMdd");

                BUILDCACHE = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>>();
                BUILDREPLAYSCACHE = new Dictionary<string, Dictionary<string, Dictionary<string, List<dsreplay>>>>();
                BUILDDURCACHE = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>();
                BUILDWRCACHE = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>>();
                FILTER = new Dictionary<string, Dictionary<string, Dictionary<string, DSfilter>>>();
                FILTER.Add("Winrate", new Dictionary<string, Dictionary<string, DSfilter>>());
                FILTER["Winrate"].Add("0", new Dictionary<string, DSfilter>());
                FILTER["Winrate"]["0"].Add("0", new DSfilter());
                BuildUpdateNeeded = true;
            });
        }

        public async Task InitBuilds()
        {
            if (BuildUpdateNeeded == false) return;

            await Task.Run(() =>
            {
                lock (BUILD_REPLAYS)
                    lock (DSdata.Replays)
                    {
                        BuildUpdateNeeded = false;
                        BUILD_REPLAYS = new Dictionary<string, List<dsreplay>>();
                        BUILD_REPLAYS.Add("ALL", DSdata.Replays);
                        //BUILD_REPLAYS.Add("player", DSdata.Replays);

                        foreach (var ent in DSdata.s_builds_hash)
                        {
                            BUILD_REPLAYS.Add(ent.Value, new List<dsreplay>(DSdata.Replays.Where(x => x.PLDupPos.Keys.Contains(ent.Key))));
                            foreach (dsreplay replay in BUILD_REPLAYS[ent.Value].ToArray())
                            {
                                int plpos = replay.PLAYERS.FindIndex(s => s.NAME == "player");
                                if (replay.PLDupPos.ContainsKey(ent.Key) && plpos != replay.PLDupPos[ent.Key])
                                {
                                    string srep = JsonSerializer.Serialize(replay);
                                    dsreplay rrep = JsonSerializer.Deserialize<dsreplay>(srep);
                                    string rname = rrep.PLAYERS[replay.PLDupPos[ent.Key]].NAME;
                                    rrep.PLAYERS[replay.PLDupPos[ent.Key]].NAME = "player";
                                    rrep.PLAYERS[plpos].NAME = rname;
                                    BUILD_REPLAYS[ent.Value].Remove(replay);
                                    BUILD_REPLAYS[ent.Value].Add(rrep);
                                }
                            }
                        }

                        foreach (string player in BUILD_REPLAYS.Keys.ToArray())
                        {
                            GenBuilds(player);
                        }
                    }
            });
            BuildUpdateNeeded = false;
        }

        public void GenBuilds(string player, DSoptions opt = null)
        {
            if (opt == null) {
                opt = new DSoptions();
                opt.Startdate = new DateTime(2019, 1, 1);
            }

            string startdate = opt.Startdate.ToString("yyyyMMdd");
            string enddate = opt.Enddate.ToString("yyyyMMdd");

            if (!BUILDREPLAYSCACHE.ContainsKey(player)) BUILDREPLAYSCACHE.Add(player, new Dictionary<string, Dictionary<string, List<dsreplay>>>());
            else BUILDREPLAYSCACHE[player] = new Dictionary<string, Dictionary<string, List<dsreplay>>>();

            List<dsreplay> myreplays = new List<dsreplay>();
            if (player == "ALL") myreplays = DSdata.Replays;
            else if (BUILD_REPLAYS.ContainsKey(player)) myreplays = BUILD_REPLAYS[player];

            (List<dsreplay> replays, DSfilter fil) = DBfilter.Filter(myreplays, opt);
            
            
            if (!FILTER.ContainsKey("Builds")) FILTER.Add("Builds", new Dictionary<string, Dictionary<string, DSfilter>>());
            if (!FILTER["Builds"].ContainsKey(startdate)) FILTER["Builds"].Add(startdate, new Dictionary<string, DSfilter>());
            if (!FILTER["Builds"][startdate].ContainsKey(enddate)) FILTER["Builds"][startdate].Add(enddate, fil);

            int games = 0;
            int rgames = 0;
            int ygames = 0;
            int wins = 0;

            Dictionary<string, Dictionary<string, Dictionary<string, int>>> GAMES = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> WINS = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, double>>> DURATION = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, int>>>> UNITS = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, int>>>>();
            //init
            foreach (string bp in DSdata.s_breakpoints)
            {
                GAMES.Add(bp, new Dictionary<string, Dictionary<string, int>>());
                WINS.Add(bp, new Dictionary<string, Dictionary<string, int>>());
                DURATION.Add(bp, new Dictionary<string, Dictionary<string, double>>());
                UNITS.Add(bp, new Dictionary<string, Dictionary<string, Dictionary<string, int>>>());

                foreach (string cmdr in DSdata.s_races)
                {
                    GAMES[bp].Add(cmdr, new Dictionary<string, int>());
                    WINS[bp].Add(cmdr, new Dictionary<string, int>());
                    DURATION[bp].Add(cmdr, new Dictionary<string, double>());
                    UNITS[bp].Add(cmdr, new Dictionary<string, Dictionary<string, int>>());
                    if (bp == "ALL") BUILDREPLAYSCACHE[player].Add(cmdr, new Dictionary<string, List<dsreplay>>());

                    foreach (string vs in DSdata.s_races)
                    {
                        GAMES[bp][cmdr].Add(vs, 0);
                        WINS[bp][cmdr].Add(vs, 0);
                        DURATION[bp][cmdr].Add(vs, 0);
                        UNITS[bp][cmdr].Add(vs, new Dictionary<string, int>());
                        if (bp == "ALL") BUILDREPLAYSCACHE[player][cmdr].Add(vs, new List<dsreplay>());
                    }
                    GAMES[bp][cmdr].Add("ALL", 0);
                    WINS[bp][cmdr].Add("ALL", 0);
                    DURATION[bp][cmdr].Add("ALL", 0);
                    UNITS[bp][cmdr].Add("ALL", new Dictionary<string, int>());
                    if (bp == "ALL") BUILDREPLAYSCACHE[player][cmdr].Add("ALL", new List<dsreplay>());
                }
            }


            foreach (dsreplay rep in replays)
            {
                //if (rep.PLAYERCOUNT != 6) continue;
                if (rep.ISBRAWL == true) continue;
                foreach (dsplayer pl in rep.PLAYERS)
                {
                    foreach (string bp in DSdata.s_breakpoints)
                    {
                        if (!pl.UNITS.ContainsKey(bp)) continue;
                        dsplayer opp = rep.GetOpp(pl.REALPOS);
                        if (opp == null || opp.RACE == null) continue;

                        //if (player == "player" && !opt.Players.Where(p => p.Value == true).Select(s => s.Key).Contains(pl.NAME)) continue;
                        if (pl.NAME == "player")
                        {
                            if (bp == "ALL")
                            {
                                BUILDREPLAYSCACHE[player][pl.RACE]["ALL"].Add(rep);
                                BUILDREPLAYSCACHE[player][pl.RACE][opp.RACE].Add(rep);
                            }
                            if (pl.UNITS.ContainsKey(bp))
                            {
                                games++;
                                GAMES[bp][pl.RACE]["ALL"]++;
                                GAMES[bp][pl.RACE][opp.RACE]++;
                                if (pl.TEAM == rep.WINNER)
                                {
                                    wins++;
                                    WINS[bp][pl.RACE]["ALL"]++;
                                    WINS[bp][pl.RACE][opp.RACE]++;
                                }

                                foreach (string unit in pl.UNITS[bp].Keys.ToArray())
                                {
                                    if (unit.StartsWith("Decoration")) continue;

                                    bool isBrawl = false;
                                    if (unit.StartsWith("Hybrid")) isBrawl = true;
                                    else if (unit.StartsWith("MercCamp")) isBrawl = true;

                                    if (isBrawl) continue;

                                    if (unit == "TychusTychus")
                                    {
                                        if (pl.UNITS[bp][unit] > 1) pl.UNITS[bp][unit] = 1;
                                    }

                                    string fixunit = FixUnitName(unit);
                                    //ALLUNITS.Add(fixunit);

                                    if (fixunit == "") continue;

                                    if (UNITS[bp][pl.RACE]["ALL"].ContainsKey(fixunit)) UNITS[bp][pl.RACE]["ALL"][fixunit] += pl.UNITS[bp][unit];
                                    else UNITS[bp][pl.RACE]["ALL"].Add(fixunit, pl.UNITS[bp][unit]);

                                    if (UNITS[bp][pl.RACE][opp.RACE].ContainsKey(fixunit)) UNITS[bp][pl.RACE][opp.RACE][fixunit] += pl.UNITS[bp][unit];
                                    else UNITS[bp][pl.RACE][opp.RACE].Add(fixunit, pl.UNITS[bp][unit]);


                                }
                                DURATION[bp][pl.RACE]["ALL"] += rep.DURATION;
                                DURATION[bp][pl.RACE][opp.RACE] += rep.DURATION;
                            }
                        }
                    }
                }
            }

            Console.WriteLine(replays.Count + " " + player + " Games: " + games + "|" + rgames + "|" + ygames + " Wins: " + wins);

            // build, cmdr, cmdr_vs, breakpoint, unit, count | wr, games
            //public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>> BUILDCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>>();
            //public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>> BUILDWRCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>>();

            foreach (string bp in DSdata.s_breakpoints)
            {
                foreach (string cmdr in DSdata.s_races)
                {
                    double gwr = 0;
                    if (WINS.ContainsKey(bp) && WINS[bp].ContainsKey(cmdr) && WINS[bp][cmdr].ContainsKey("ALL") &&
                        GAMES.ContainsKey(bp) && GAMES[bp].ContainsKey(cmdr) && GAMES[bp][cmdr].ContainsKey("ALL"))
                    {
                        gwr = DSDbStats.GenWr(WINS[bp][cmdr]["ALL"], GAMES[bp][cmdr]["ALL"]);
                    }
                    double gdur = 0;
                    if (DURATION.ContainsKey(bp) && DURATION[bp].ContainsKey(cmdr) && DURATION[bp][cmdr].ContainsKey("ALL"))
                    {
                        if (GAMES[bp][cmdr]["ALL"] > 0) gdur = DURATION[bp][cmdr]["ALL"] / GAMES[bp][cmdr]["ALL"] / 22.4;
                    }

                    if (!BUILDDURCACHE.ContainsKey(player)) BUILDDURCACHE.Add(player, new Dictionary<string, Dictionary<string, Dictionary<string, double>>>());
                    if (!BUILDDURCACHE[player].ContainsKey(cmdr)) BUILDDURCACHE[player].Add(cmdr, new Dictionary<string, Dictionary<string, double>>());
                    if (!BUILDDURCACHE[player][cmdr].ContainsKey("ALL")) BUILDDURCACHE[player][cmdr].Add("ALL", new Dictionary<string, double>());
                    if (!BUILDDURCACHE[player][cmdr]["ALL"].ContainsKey(bp)) BUILDDURCACHE[player][cmdr]["ALL"].Add(bp, 0);
                    BUILDDURCACHE[player][cmdr]["ALL"][bp] = gdur;

                    if (!BUILDWRCACHE.ContainsKey(player)) BUILDWRCACHE.Add(player, new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>());
                    if (!BUILDWRCACHE[player].ContainsKey(cmdr)) BUILDWRCACHE[player].Add(cmdr, new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>());
                    if (!BUILDWRCACHE[player][cmdr].ContainsKey("ALL")) BUILDWRCACHE[player][cmdr].Add("ALL", new Dictionary<string, KeyValuePair<double, int>>());
                    if (!BUILDWRCACHE[player][cmdr]["ALL"].ContainsKey(bp)) BUILDWRCACHE[player][cmdr]["ALL"].Add(bp, new KeyValuePair<double, int>());
                    BUILDWRCACHE[player][cmdr]["ALL"][bp] = new KeyValuePair<double, int>(gwr, GAMES[bp][cmdr]["ALL"]);

                    if (!BUILDCACHE.ContainsKey(player)) BUILDCACHE.Add(player, new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>());
                    if (!BUILDCACHE[player].ContainsKey(cmdr)) BUILDCACHE[player].Add(cmdr, new Dictionary<string, Dictionary<string, Dictionary<string, double>>>());
                    if (!BUILDCACHE[player][cmdr].ContainsKey("ALL")) BUILDCACHE[player][cmdr].Add("ALL", new Dictionary<string, Dictionary<string, double>>());
                    if (!BUILDCACHE[player][cmdr]["ALL"].ContainsKey(bp)) BUILDCACHE[player][cmdr]["ALL"].Add(bp, new Dictionary<string, double>());

                    if (UNITS.ContainsKey(bp) && UNITS[bp].ContainsKey(cmdr) && UNITS[bp][cmdr].ContainsKey("ALL"))
                    {
                        foreach (string unit in UNITS[bp][cmdr]["ALL"].Keys)
                        {
                            double ucount = 0;
                            ucount = (double)UNITS[bp][cmdr]["ALL"][unit] / (double)GAMES[bp][cmdr]["ALL"];

                            if (!BUILDCACHE[player][cmdr]["ALL"].ContainsKey(unit)) BUILDCACHE[player][cmdr]["ALL"][bp].Add(unit, ucount);
                            else BUILDCACHE[player][cmdr]["ALL"][bp][unit] = ucount;
                        }
                    }

                    foreach (string vs in DSdata.s_races)
                    {
                        double wr = 0;
                        if (WINS.ContainsKey(bp) && WINS[bp].ContainsKey(cmdr) && WINS[bp][cmdr].ContainsKey(vs) &&
                            GAMES.ContainsKey(bp) && GAMES[bp].ContainsKey(cmdr) && GAMES[bp][cmdr].ContainsKey(vs))
                        {
                            wr = DSDbStats.GenWr(WINS[bp][cmdr][vs], GAMES[bp][cmdr][vs]);
                        }
                        double dur = 0;
                        if (DURATION.ContainsKey(bp) && DURATION[bp].ContainsKey(cmdr) && DURATION[bp][cmdr].ContainsKey(vs))
                        {
                            if (GAMES[bp][cmdr][vs] > 0) dur = DURATION[bp][cmdr][vs] / GAMES[bp][cmdr][vs] / 22.4;
                        }

                        if (!BUILDDURCACHE[player][cmdr].ContainsKey(vs)) BUILDDURCACHE[player][cmdr].Add(vs, new Dictionary<string, double>());
                        if (!BUILDDURCACHE[player][cmdr][vs].ContainsKey(bp)) BUILDDURCACHE[player][cmdr][vs].Add(bp, 0);
                        BUILDDURCACHE[player][cmdr][vs][bp] = dur;

                        if (!BUILDWRCACHE[player][cmdr].ContainsKey(vs)) BUILDWRCACHE[player][cmdr].Add(vs, new Dictionary<string, KeyValuePair<double, int>>());
                        if (!BUILDWRCACHE[player][cmdr][vs].ContainsKey(bp)) BUILDWRCACHE[player][cmdr][vs].Add(bp, new KeyValuePair<double, int>());
                        BUILDWRCACHE[player][cmdr][vs][bp] = new KeyValuePair<double, int>(wr, GAMES[bp][cmdr][vs]);

                        if (!BUILDCACHE[player][cmdr].ContainsKey(vs)) BUILDCACHE[player][cmdr].Add(vs, new Dictionary<string, Dictionary<string, double>>());
                        if (!BUILDCACHE[player][cmdr][vs].ContainsKey(bp)) BUILDCACHE[player][cmdr][vs].Add(bp, new Dictionary<string, double>());

                        if (UNITS.ContainsKey(bp) && UNITS[bp].ContainsKey(cmdr) && UNITS[bp][cmdr].ContainsKey(vs))
                        {
                            foreach (string unit in UNITS[bp][cmdr][vs].Keys)
                            {
                                double ucount = 0;
                                ucount = (double)UNITS[bp][cmdr][vs][unit] / (double)GAMES[bp][cmdr][vs];

                                if (!BUILDCACHE[player][cmdr][vs].ContainsKey(unit)) BUILDCACHE[player][cmdr][vs][bp].Add(unit, ucount);
                                else BUILDCACHE[player][cmdr][vs][bp][unit] = ucount;
                            }
                        }
                    }
                }
            }

        }

        public string FixUnitName(string unit)
        {
            if (unit == "TrophyRiftPremium") return "";
            // abathur unknown
            if (unit == "ParasiticBombRelayDummy") return "";
            // raynor viking
            if (unit == "VikingFighter" || unit == "VikingAssault") return "Viking";
            if (unit == "DuskWings") return "DuskWing";
            // stukov lib
            if (unit == "InfestedLiberatorViralSwarm") return "InfestedLiberator";
            // Tychus extra mins
            if (unit == "MineralIncome") return "";
            // Zagara
            if (unit == "InfestedAbomination") return "Aberration";
            // Horner viking
            if (unit == "HornerDeimosVikingFighter" || unit == "HornerDeimosVikingAssault") return "HornerDeimosViking";
            if (unit == "HornerAssaultGalleonUpgraded") return "HornerAssaultGalleon";
            // Terrran thor
            if (unit == "ThorAP") return "Thor";

            foreach (string cmdr in DSdata.s_races_cmdr)
            {
                if (unit.EndsWith(cmdr))
                    return unit.Replace(cmdr, "");
                if (unit.StartsWith(cmdr))
                    return unit.Replace(cmdr, "");
            }

            Match m = rx_star.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString() + m.Groups[2].ToString();

            m = rx_light.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString() + m.Groups[2].ToString();

            m = rx_hero.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString();

            m = rx_mp.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString();

            return unit;
        }
    }
}
