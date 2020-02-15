using System;
using System.Collections.Generic;
using System.Text;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using sc2dsstats.lib.Db;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using sc2dsstats.lib.Data;
using System.Collections.Concurrent;

namespace sc2dsstats.shared.Service
{
    public static class BuildService
    {
        static Regex rx_star = new Regex(@"(.*)Starlight(.*)", RegexOptions.Singleline);
        static Regex rx_light = new Regex(@"(.*)Lightweight(.*)", RegexOptions.Singleline);
        static Regex rx_hero = new Regex(@"Hero(.*)WaveUnit", RegexOptions.Singleline);
        static Regex rx_mp = new Regex(@"(.*)MP$", RegexOptions.Singleline);

        internal static ConcurrentDictionary<string, BuildResult> BuildCache { get; set; } = new ConcurrentDictionary<string, BuildResult>();
        internal static ConcurrentBag<string> Computing { get; set; } = new ConcurrentBag<string>();

        public static void Reset()
        {
            BuildCache = new ConcurrentDictionary<string, BuildResult>();
            Computing = new ConcurrentBag<string>();
        }

        public static async Task<BuildResult> GetBuild(DSoptions _options)
        {
            string Hash = _options.GenHash();

            if (BuildCache.ContainsKey(Hash))
                return BuildCache[Hash];

            bool doWait = false;
            lock (Computing)
            {
                if (!Computing.Contains(Hash))
                    Computing.Add(Hash);
                else
                    doWait = true;
            }

            if (doWait)
            {
                while (Computing.Contains(Hash))
                {
                    await Task.Delay(500);

                }
                return BuildCache[Hash];
            }

            BuildResult bresult = new BuildResult();

            using (var context = new DSReplayContext())
            {
                var replays = DBReplayFilter.Filter(_options, context);
                bresult.TotalGames = replays.Count();

                var result = await Task.Run(() => {
                    if (String.IsNullOrEmpty(_options.Vs))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.RACE == _options.Interest
                               where t1.NAME == _options.Dataset
                               from u1 in t1.DSUnit
                               where u1.BP == _options.Breakpoint
                               select new
                               {
                                   r.ID,
                                   r.DURATION,
                                   r.GAMETIME,
                                   t1.WIN,
                                   u1.Name,
                                   u1.Count
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.RACE == _options.Interest && t1.OPPRACE == _options.Vs
                               where t1.NAME == _options.Dataset
                               from u1 in t1.DSUnit
                               where u1.BP == _options.Breakpoint
                               select new
                               {
                                   r.ID,
                                   r.DURATION,
                                   r.GAMETIME,
                                   t1.WIN,
                                   u1.Name,
                                   u1.Count
                               };
                });

                if (!result.Any())
                    return new BuildResult();
                try
                {
                    var sresult = result.Select(s => new { s.ID, s.GAMETIME, s.DURATION });
                    var lsresult = sresult.Distinct().ToList();
                    bresult.RepIDs = lsresult.Select(d => new KeyValuePair<int, string>(d.ID, d.GAMETIME.ToString("yyyy/MM/dd"))).ToList();

                    bresult.Games = bresult.RepIDs.Count;
                    float wins = result.Where(x => x.WIN == true).Select(s => s.ID).Distinct().Count();
                    bresult.Winrate = MathF.Round(wins * 100 / bresult.Games, 2);

                    var nndur = lsresult.Sum(s => s.DURATION.Ticks);
                    bresult.Duration = TimeSpan.FromTicks(nndur) / (float)bresult.Games;

                } catch (Exception e)
                {
                    Console.WriteLine("this should not happen :(");
                    return new BuildResult();
                }

                HashSet<string> units = result.Select(s => s.Name).ToHashSet();
                foreach (string unit in units)
                {
                    if (unit == "Mid") continue;
                    bresult.Units.Add(new KeyValuePair<string, float>(unit, MathF.Round((float)result.Where(x => x.Name == unit).Sum(s => s.Count) / bresult.Games, 2)));
                    if (unit == "Upgrades")
                    {
                        bresult.Upgradespending = (int)bresult.Units.Last().Value;
                        bresult.Units.RemoveAt(bresult.Units.Count - 1);
                    }
                    else if (unit == "Gas")
                    {
                        bresult.Gascount = bresult.Units.Last().Value;
                        bresult.Units.RemoveAt(bresult.Units.Count - 1);
                    }
                }
            }

            lock (BuildCache)
            {
                BuildCache[Hash] = bresult;
            }
            _ = Computing.TryTake(out Hash);

            return bresult;
        }

        public static string FixUnitName(string unit)
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
            if (unit == "TychusTychus") return "Tychus";

            foreach (string cmdr in DSdata.s_races_cmdr)
            {
                if (unit.EndsWith(cmdr))
                    return unit.Replace(cmdr, "");
                else if (unit.StartsWith(cmdr))
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

    public class BuildResult
    {
        public int TotalGames { get; set; } = 0;
        public int Games { get; set; } = 0;
        public List<KeyValuePair<string, float>> Units { get; set; } = new List<KeyValuePair<string, float>>();
        public float Winrate { get; set; } = 0.0f;
        public int Upgradespending { get; set; } = 0;
        public float Gascount { get; set; } = 0;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public List<KeyValuePair<int, string>> RepIDs { get; set; } = new List<KeyValuePair<int, string>>();
    }
}
