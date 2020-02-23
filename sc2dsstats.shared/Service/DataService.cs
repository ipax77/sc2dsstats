using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.shared.Service
{
    public static class DataService
    {
        private static ConcurrentDictionary<string, DataResult> WinrateCache { get; set; } = new ConcurrentDictionary<string, DataResult>();
        private static HashSet<string> Computing { get; set; } = new HashSet<string>();

        public static void Reset()
        {
            WinrateCache = new ConcurrentDictionary<string, DataResult>();
            Computing = new HashSet<string>();
        }

        public static async Task<DataResult> GetData(DSoptions _options, IJSRuntime _jsRuntime, bool doit = true)
        {
            string Hash = _options.GenHash();

            if (WinrateCache.ContainsKey(Hash))
                return WinrateCache[Hash];

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
                int fs = 0;
                while (Computing.Contains(Hash))
                {
                    await Task.Delay(500);
                    fs += 500;
                    if (fs > 10000)
                    {
                        return null;
                    }
                }
                if (WinrateCache.ContainsKey(Hash))
                    return WinrateCache[Hash];
            }

            //fs
            //if (!WinrateCache.Any())
            //    await Task.Delay(1000);

            DataResult dresult = new DataResult();
            Object lockobject = new object();



            var replays = DBReplayFilter.Filter(_options, _options.db, false);

            HashSet<string> allcmdrs = _options.Chart.s_races.ToHashSet();
            allcmdrs.ExceptWith(_options.Chart.s_races_ordered.ToHashSet());
             
            foreach (string race in _options.Chart.s_races_ordered.ToArray())
            {
                double wr = 0;
                int count = 0;
                (wr, count) = (_options.Mode switch
                {
                    "Winrate" => GetWinrate(_options, _options.db, replays, dresult.CmdrInfo, race),
                    "MVP" => GetMVP(_options, _options.db, replays, dresult.CmdrInfo, race),
                    "DPS" => GetDPS(_options, _options.db, replays, dresult.CmdrInfo, race),
                    "Synergy" => GetSynergy(_options, _options.db, replays, dresult.CmdrInfo, race),
                    "AntiSynergy" => GetAntiSynergy(_options, _options.db, replays, dresult.CmdrInfo, race),
                    "Timeline" => GetTimeline(_options, _options.db, replays, dresult.CmdrInfo, race),
                    _ => throw new ArgumentException(message: "invalid mode value", paramName: nameof(_options.Mode)),
                });
                if (count == 0)
                {
                    _options.Chart.s_races_ordered.Remove(race);
                    continue;
                }

                dresult.Labels.Add(race + " " + count);
                dresult.Dataset.data.Add(wr);

                string cmdr = race;
                if (!String.IsNullOrEmpty(_options.Interest))
                    cmdr = _options.Interest;
                ChartService.SetColor(dresult.Dataset, _options.Chart.type, cmdr);

                if (_options.Chart.type == "bar")
                    dresult.Images.Add(new ChartJSPluginlabelsImage(race));

                string jimage = "";
                if (_options.Chart.type == "bar")
                    if (dresult.Images.LastOrDefault() != null)
                        jimage = JsonConvert.SerializeObject(dresult.Images.Last());

                ChartJSInterop.AddData(_jsRuntime,
                    dresult.Labels.LastOrDefault(),
                    dresult.Dataset.data.Last(),
                    dresult.Dataset.backgroundColor.Last(),
                    jimage,
                    lockobject
                    );
            }

            if (allcmdrs.Any())
            {
                foreach (string race in allcmdrs)
                {
                    double wr = 0;
                    int count = 0;
                    (wr, count) = (_options.Mode switch
                    {
                        "Winrate" => GetWinrate(_options, _options.db, replays, dresult.CmdrInfo, race),
                        "MVP" => GetMVP(_options, _options.db, replays, dresult.CmdrInfo, race),
                        "DPS" => GetDPS(_options, _options.db, replays, dresult.CmdrInfo, race),
                        "Synergy" => GetSynergy(_options, _options.db, replays, dresult.CmdrInfo, race),
                        "AntiSynergy" => GetAntiSynergy(_options, _options.db, replays, dresult.CmdrInfo, race),
                        "Timeline" => GetTimeline(_options, _options.db, replays, dresult.CmdrInfo, race),
                        _ => throw new ArgumentException(message: "invalid mode value", paramName: nameof(_options.Mode)),
                    });
                    if (count == 0)
                        continue;

                    dresult.Labels.Add(race + " " + count);
                    dresult.Dataset.data.Add(wr);
                    string cmdr = race;
                    if (!String.IsNullOrEmpty(_options.Interest))
                        cmdr = _options.Interest;
                    ChartService.SetColor(dresult.Dataset, _options.Chart.type, cmdr);

                    if (_options.Chart.type == "bar")
                        dresult.Images.Add(new ChartJSPluginlabelsImage(race));
                }
            }

            if (dresult.CmdrInfo.Games > 0)
                dresult.CmdrInfo.ADuration /= dresult.CmdrInfo.Games;

            dresult.CmdrInfo.AWinrate = (_options.Mode switch
            {
                "DPS" => Math.Round(dresult.CmdrInfo.Wins / dresult.CmdrInfo.Games, 2),
                _ => Math.Round(dresult.CmdrInfo.Wins * 100 / dresult.CmdrInfo.Games, 2),
            });

            dresult.CmdrInfo.Games = dresult.CmdrInfo.GameIDs.Count();
            dresult.CmdrInfo.GameIDs = new HashSet<int>();
            _options.Cmdrinfo = new CmdrInfo(dresult.CmdrInfo);

            lock (WinrateCache)
            {
                WinrateCache[Hash] = dresult;
            }
            lock (Computing)
            {
                Computing.Remove(Hash);
            }
            _options.Chart.data.labels = new List<string>(dresult.Labels);

            if (String.IsNullOrEmpty(_options.Interest))
            {
                dresult.Dataset.label = "global";
                if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy" || _options.Mode == "Timeline")
                {
                    _options.Interest = "Abathur";
                    dresult.Dataset.label = _options.Interest;
                    _options.CmdrsChecked["Abathur"] = true;
                }
            }
            else
                dresult.Dataset.label = _options.Interest;

            _options.Chart.data.datasets[_options.Chart.data.datasets.Count - 1] = dresult.Dataset;
            if (_options.Chart.type == "bar")
            {
                var options = _options.Chart.options as ChartJsoptionsBar;
                options.plugins.labels.images = dresult.Images;
            }
            _options.Cmdrinfo = dresult.CmdrInfo;
            return (null);
        }

        public static (double, int) GetWinrate(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = (String.IsNullOrEmpty(_options.Interest), _options.Player, _options.Dataset.Any()) switch
            {
               (true, true, false) => from r in replays
                                      from t1 in r.DSPlayer
                                      where t1.NAME.Length == 64 && t1.RACE == cmdr
                                      select new
                                      {
                                          t1.WIN,
                                          r.DURATION,
                                          r.ID
                                      },
                (true, true, true) => from r in replays
                                      from t1 in r.DSPlayer
                                      where _options.Dataset.Contains(t1.NAME) && t1.RACE == cmdr
                                      select new
                                      {
                                          t1.WIN,
                                          r.DURATION,
                                          r.ID
                                      },
              (true, false, false) => from r in replays
                                      from t1 in r.DSPlayer
                                      where t1.RACE == cmdr
                                      select new
                                      {
                                          t1.WIN,
                                          r.DURATION,
                                          r.ID
                                      },
                (true, false, true) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == cmdr
                                        select new
                                        {
                                            t1.WIN,
                                            r.DURATION,
                                            r.ID
                                        },
                (false, true, false) => from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.NAME.Length == 64 && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.WIN,
                                         r.DURATION,
                                         r.ID
                                     },
              (false, true, true) => from r in replays
                                     from t1 in r.DSPlayer
                                     where _options.Dataset.Contains(t1.NAME) && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.WIN,
                                         r.DURATION,
                                         r.ID
                                     },
             (false, false, true) => from r in replays
                                     from t1 in r.DSPlayer
                                     where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                     select new
                                     {
                                         t1.WIN,
                                         r.DURATION,
                                         r.ID
                                     },
               (false, false, false) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                        select new
                                        {
                                            t1.WIN,
                                            r.DURATION,
                                            r.ID
                                        }
            };

            var resultlist = result.ToList();
            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromSeconds(resultlist.Sum(s => s.DURATION));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static (double, int) GetMVP(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = (String.IsNullOrEmpty(_options.Interest), _options.Player, _options.Dataset.Any()) switch
            {
                (true, true, false) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.NAME.Length == 64 && t1.RACE == cmdr
                                       select new
                                       {
                                           t1.KILLSUM,
                                           r.MAXKILLSUM,
                                           r.DURATION,
                                           r.ID
                                       },
                (true, true, true) => from r in replays
                                      from t1 in r.DSPlayer
                                      where _options.Dataset.Contains(t1.NAME) && t1.RACE == cmdr
                                      select new
                                      {
                                          t1.KILLSUM,
                                          r.MAXKILLSUM,
                                          r.DURATION,
                                          r.ID
                                      },
                (true, false, false) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == cmdr
                                        select new
                                        {
                                            t1.KILLSUM,
                                            r.MAXKILLSUM,
                                            r.DURATION,
                                            r.ID
                                        },
                (true, false, true) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.RACE == cmdr
                                       select new
                                       {
                                           t1.KILLSUM,
                                           r.MAXKILLSUM,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, true, false) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.NAME.Length == 64 && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                        select new
                                        {
                                            t1.KILLSUM,
                                            r.MAXKILLSUM,
                                            r.DURATION,
                                            r.ID
                                        },
                (false, true, true) => from r in replays
                                       from t1 in r.DSPlayer
                                       where _options.Dataset.Contains(t1.NAME) && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                       select new
                                       {
                                           t1.KILLSUM,
                                           r.MAXKILLSUM,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, false, true) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                        select new
                                        {
                                            t1.KILLSUM,
                                            r.MAXKILLSUM,
                                            r.DURATION,
                                            r.ID
                                        },
                (false, false, false) => from r in replays
                                         from t1 in r.DSPlayer
                                         where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                         select new
                                         {
                                             t1.KILLSUM,
                                             r.MAXKILLSUM,
                                             r.DURATION,
                                             r.ID
                                         }
            };
            var resultlist = result.ToList();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
            duration = TimeSpan.FromSeconds(resultlist.Sum(s => s.DURATION));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static (double, int) GetDPS(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = (String.IsNullOrEmpty(_options.Interest), _options.Player, _options.Dataset.Any()) switch
            {
                (true, true, false) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.NAME.Length == 64 && t1.RACE == cmdr
                                       select new
                                       {
                                           t1.KILLSUM,
                                           t1.ARMY,
                                           r.DURATION,
                                           r.ID
                                       },
                (true, true, true) => from r in replays
                                      from t1 in r.DSPlayer
                                      where _options.Dataset.Contains(t1.NAME) && t1.RACE == cmdr
                                      select new
                                      {
                                          t1.KILLSUM,
                                          t1.ARMY,
                                          r.DURATION,
                                          r.ID
                                      },
                (true, false, false) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == cmdr
                                        select new
                                        {
                                            t1.KILLSUM,
                                            t1.ARMY,
                                            r.DURATION,
                                            r.ID
                                        },
                (true, false, true) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.RACE == cmdr
                                       select new
                                       {
                                           t1.KILLSUM,
                                           t1.ARMY,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, true, false) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.NAME.Length == 64 && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                        select new
                                        {
                                            t1.KILLSUM,
                                            t1.ARMY,
                                            r.DURATION,
                                            r.ID
                                        },
                (false, true, true) => from r in replays
                                       from t1 in r.DSPlayer
                                       where _options.Dataset.Contains(t1.NAME) && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                       select new
                                       {
                                           t1.KILLSUM,
                                           t1.ARMY,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, false, true) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                        select new
                                        {
                                            t1.KILLSUM,
                                            t1.ARMY,
                                            r.DURATION,
                                            r.ID
                                        },
                (false, false, false) => from r in replays
                                         from t1 in r.DSPlayer
                                         where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                                         select new
                                         {
                                             t1.KILLSUM,
                                             t1.ARMY,
                                             r.DURATION,
                                             r.ID
                                         }
            };

            var resultlist = result.ToList();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
            duration = TimeSpan.FromSeconds(resultlist.Sum(s => s.DURATION));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins / games, 2), (int)games);
        }
        public static (double, int) GetSynergy(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {

            var result = (String.IsNullOrEmpty(_options.Interest), _options.Player, _options.Dataset.Any()) switch
            {
                (false, true, false) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.NAME.Length == 64 && t1.RACE == _options.Interest
                                       from t3 in r.DSPlayer
                                       where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                                       select new
                                       {
                                           t1.WIN,
                                           r.DURATION,
                                           r.ID
                                       },
              (false, true, true) => from r in replays
                                      from t1 in r.DSPlayer
                                      where _options.Dataset.Contains(t1.NAME) && t1.RACE == _options.Interest
                                      from t3 in r.DSPlayer
                                      where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                                      select new
                                      {
                                          t1.WIN,
                                          r.DURATION,
                                          r.ID
                                      },
             (false, false, true) => from r in replays
                                    from t1 in r.DSPlayer
                                    where t1.RACE == _options.Interest
                                    from t3 in r.DSPlayer
                                    where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                                    select new
                                    {
                                        t1.WIN,
                                        r.DURATION,
                                        r.ID
                                    },
                (false, false, false) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.RACE == _options.Interest
                                       from t3 in r.DSPlayer
                                       where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                                       select new
                                       {
                                           t1.WIN,
                                           r.DURATION,
                                           r.ID
                                       },
                            _ => throw new NotImplementedException()
            };
            var resultlist = result.ToList();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromSeconds(resultlist.Sum(s => s.DURATION));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static (double, int) GetAntiSynergy(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = (String.IsNullOrEmpty(_options.Interest), _options.Player, _options.Dataset.Any()) switch
            {
                (false, true, false) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.NAME.Length == 64 && t1.RACE == _options.Interest
                                       from t3 in r.DSPlayer
                                       where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                                       select new
                                       {
                                           t1.WIN,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, true, true) => from r in replays
                                        from t1 in r.DSPlayer
                                        where _options.Dataset.Contains(t1.NAME) && t1.RACE == _options.Interest
                                        from t3 in r.DSPlayer
                                        where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                                        select new
                                        {
                                            t1.WIN,
                                            r.DURATION,
                                            r.ID
                                        },
                (false, false, true) => from r in replays
                                       from t1 in r.DSPlayer
                                       where t1.RACE == _options.Interest
                                       from t3 in r.DSPlayer
                                       where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                                       select new
                                       {
                                           t1.WIN,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, false, false) => from r in replays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == _options.Interest
                                        from t3 in r.DSPlayer
                                        where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                                        select new
                                        {
                                            t1.WIN,
                                            r.DURATION,
                                            r.ID
                                        },
                _ => throw new NotImplementedException()
            };
            var resultlist = result.ToList();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromSeconds(resultlist.Sum(s => s.DURATION));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static (double, int) GetTimeline(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            DateTime t = DateTime.ParseExact(cmdr, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var treplays = replays.Where(x => x.GAMETIME > t.AddDays(-7) && x.GAMETIME < t);

            var result = (String.IsNullOrEmpty(_options.Interest), _options.Player, _options.Dataset.Any()) switch
            {
                (false, true, false) => from r in treplays
                                       from t1 in r.DSPlayer
                                       where t1.NAME.Length == 64 && t1.RACE == _options.Interest
                                       select new
                                       {
                                           t1.WIN,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, true, true) => from r in treplays
                                        from t1 in r.DSPlayer
                                        where _options.Dataset.Contains(t1.NAME) && t1.RACE == _options.Interest
                                        select new
                                        {
                                            t1.WIN,
                                            r.DURATION,
                                            r.ID
                                        },
                (false, false, true) => from r in treplays
                                       from t1 in r.DSPlayer
                                       where t1.RACE == _options.Interest
                                       select new
                                       {
                                           t1.WIN,
                                           r.DURATION,
                                           r.ID
                                       },
                (false, false, false) => from r in treplays
                                        from t1 in r.DSPlayer
                                        where t1.RACE == _options.Interest
                                        select new
                                        {
                                            t1.WIN,
                                            r.DURATION,
                                            r.ID
                                        },
                _ => throw new NotImplementedException()
            };
            var resultlist = result.ToList();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromSeconds(resultlist.Sum(s => s.DURATION));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
    }

    public class DataResult
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<ChartJSPluginlabelsImage> Images { get; set; } = new List<ChartJSPluginlabelsImage>();
        public ChartJSdataset Dataset { get; set; } = new ChartJSdataset("Default");
        public CmdrInfo CmdrInfo { get; set; } = new CmdrInfo();
    }
}
