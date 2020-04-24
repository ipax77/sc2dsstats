using Microsoft.Extensions.Options;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.shared.Service
{
    public static class BuildService
    {


        private static ConcurrentDictionary<string, BuildResult> BuildCache { get; set; } = new ConcurrentDictionary<string, BuildResult>();
        private static HashSet<string> Computing { get; set; } = new HashSet<string>();

        public static void Reset()
        {
            BuildCache = new ConcurrentDictionary<string, BuildResult>();
            Computing = new HashSet<string>();
        }

        public static async Task GetBuild(DSoptions _options, DSReplayContext _context, object dblock)
        {
            if (_options.Vs == "ALL")
                _options.Vs = String.Empty;
            string Hash = _options.GenHash();

            if (!_options.Decoding && BuildCache.ContainsKey(Hash))
            {
                _options.buildResult = BuildCache[Hash];
                return;
            }

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
                if (BuildCache.ContainsKey(Hash))
                    _options.buildResult = BuildCache[Hash];
                return;
            }

            BuildResult bresult = new BuildResult();

            foreach (var ent in _options.Gamemodes.Keys.ToArray())
                _options.Gamemodes[ent] = false;

            if (_options.Interest == "Terran" || _options.Interest == "Zerg" || _options.Interest == "Protoss")
            {
                _options.Gamemodes["GameModeStandard"] = true;
            }
            else
            {
                _options.Gamemodes["GameModeCommanders"] = true;
                _options.Gamemodes["GameModeCommandersHeroic"] = true;
            }

            lock (dblock)
            {
                var replays = DBReplayFilter.Filter(_options, _context);
                bresult.TotalGames = replays.Count();

                var result = (String.IsNullOrEmpty(_options.Vs) switch
                {
                    true => from r in replays
                            from t1 in r.DSPlayer
                            where t1.RACE == _options.Interest
                            where _options.Dataset.Contains(t1.NAME)
                            from u1 in t1.Breakpoints
                            where u1.Breakpoint == _options.Breakpoint
                            select new
                            {
                                r.ID,
                                r.DURATION,
                                r.GAMETIME,
                                t1.WIN,
                                u1.dsUnitsString,
                                u1.Upgrades,
                                u1.Gas
                            },
                    false => from r in replays
                             from t1 in r.DSPlayer
                             where t1.RACE == _options.Interest && t1.OPPRACE == _options.Vs
                             where _options.Dataset.Contains(t1.NAME)
                             from u1 in t1.Breakpoints
                             where u1.Breakpoint == _options.Breakpoint
                             select new
                             {
                                 r.ID,
                                 r.DURATION,
                                 r.GAMETIME,
                                 t1.WIN,
                                 u1.dsUnitsString,
                                 u1.Upgrades,
                                 u1.Gas
                             }
                });
                if (!result.Any())
                {
                    _options.buildResult = new BuildResult();
                    return;
                }
                try
                {
                    var sresult = result.Select(s => new { s.ID, s.GAMETIME, s.DURATION });
                    var lsresult = sresult.Distinct().ToList();
                    bresult.RepIDs = lsresult.Select(s => new { s.ID, s.GAMETIME }).ToDictionary(d => d.ID, d => d.GAMETIME.ToString("yyyy/MM/dd"));

                    bresult.Games = bresult.RepIDs.Count;
                    float wins = result.Where(x => x.WIN == true).Select(s => s.ID).Distinct().Count();
                    bresult.Winrate = MathF.Round(wins * 100 / bresult.Games, 2);

                    var nndur = lsresult.Sum(s => s.DURATION);
                    bresult.Duration = TimeSpan.FromSeconds(nndur) / (float)bresult.Games;

                }
                catch (Exception e)
                {
                    Console.WriteLine("this should not happen :(" + e.Message);
                    _options.buildResult = new BuildResult();
                    lock (Computing)
                    {
                        Computing.Remove(Hash);
                    }
                    return;
                }

                foreach (string unitsstring in result.Select(s => s.dsUnitsString))
                {
                    if (!String.IsNullOrEmpty(unitsstring))
                    {
                        foreach (string unitstring in unitsstring.Split("|"))
                        {
                            var ent = unitstring.Split(",");

                            if (!bresult.Units.ContainsKey(ent[0]))
                                bresult.Units[ent[0]] = float.Parse(ent[1]);
                            else
                                bresult.Units[ent[0]] += float.Parse(ent[1]);
                        }
                    }
                }


                foreach (string unit in bresult.Units.Keys.ToArray())
                {
                    bresult.Units[unit] = MathF.Round(bresult.Units[unit] / bresult.Games, 2);
                    int id = 0;
                    if (int.TryParse(unit, out id))
                    {
                        UnitModelBase bunit = DSdata.Units.SingleOrDefault(s => s.ID == id);
                        if (bunit != null)
                        {
                            bresult.Units[bunit.Name] = bresult.Units[unit];
                            bresult.Units.Remove(unit);
                        }
                    }
                }

                bresult.Upgradespending = result.Sum(s => s.Upgrades) / bresult.Games;
                bresult.Gascount = MathF.Round((float)result.Sum(s => s.Gas) / bresult.Games, 2);

                bresult.UnitsOrdered = bresult.Units.OrderByDescending(o => o.Value);
                bresult.max1 = 100;
                bresult.max2 = 100;
                bresult.max3 = 100;
                if (bresult.Units.Count > 2)
                {
                    bresult.max1 = (int)bresult.UnitsOrdered.ElementAt(0).Value;
                    bresult.max2 = (int)bresult.UnitsOrdered.ElementAt(1).Value;
                    bresult.max3 = (int)bresult.UnitsOrdered.ElementAt(2).Value;
                }
            }

            lock (BuildCache)
            {
                BuildCache[Hash] = bresult;
            }

            lock (Computing)
            {
                Computing.Remove(Hash);
            }

            _options.buildResult = bresult;
        }
    }

}
