using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Globalization;

namespace sc2dsstats.shared.Service
{
    public static class DataService
    {
        internal static ConcurrentDictionary<string, DataResult> WinrateCache { get; set; } = new ConcurrentDictionary<string, DataResult>();
        internal static ConcurrentBag<string> Computing { get; set; } = new ConcurrentBag<string>();

        public static void Reset()
        {
            WinrateCache = new ConcurrentDictionary<string, DataResult>();
            Computing = new ConcurrentBag<string>();
        }

        public static async Task<DataResult> GetData(DSoptions _options, IJSRuntime _jsRuntime)
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
                while (Computing.Contains(Hash))
                {
                    await Task.Delay(500);

                }
                if (WinrateCache.ContainsKey(Hash))
                    return WinrateCache[Hash];
            }

            DataResult dresult = new DataResult();
            Object lockobject = new object();

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

            using (var context = new DSReplayContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var replays = DBReplayFilter.Filter(_options, context, false);

                // TODO: ordered 
                foreach (string race in _options.Chart.s_races_ordered)
                {
                    double wr = 0;
                    int count = 0;

                    (wr, count) = (_options.Mode switch
                    {
                        "Winrate" =>  await GetWinrate(_options, context, replays, dresult.CmdrInfo, race),
                        "MVP" => await GetMVP(_options, context, replays, dresult.CmdrInfo, race),
                        "DPS" => await GetDPS(_options, context, replays, dresult.CmdrInfo, race),
                        "Synergy" => await GetSynergy(_options, context, replays, dresult.CmdrInfo, race),
                        "AntiSynergy" => await GetAntiSynergy(_options, context, replays, dresult.CmdrInfo, race),
                        "Timeline" => await GetTimeline(_options, context, replays, dresult.CmdrInfo, race),
                        _    => throw new ArgumentException(message: "invalid mode value", paramName: nameof(_options.Mode)),
                    });
                    if (count == 0)
                        continue;

                    string cmdr = race;
                    if (!String.IsNullOrEmpty(_options.Interest))
                        cmdr = _options.Interest;

                    dresult.Labels.Add(race + " " + count);
                    dresult.Dataset.data.Add(wr);

                    
                    ChartService.SetColor(dresult.Dataset, _options.Chart.type, cmdr);

                    if (_options.Chart.type == "bar")
                        dresult.Images.Add(new ChartJSPluginlabelsImage(race));

                    if (dresult.Dataset.data.Count == 1)
                    {
                        _options.Chart.data.datasets.Add(dresult.Dataset);
                        if (_options.Chart.data.datasets.Count == 1)
                        {
                            _options.Chart.data.labels = dresult.Labels;
                            if (_options.Chart.type == "bar")
                            {
                                var options = _options.Chart.options as ChartJsoptionsBar;
                                options.plugins.labels.images = dresult.Images;
                            }
                            await ChartJSInterop.ChartChanged(_jsRuntime, JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
                        } else
                            await ChartJSInterop.AddDataset(_jsRuntime, JsonConvert.SerializeObject(dresult.Dataset), lockobject);
                    }
                    else
                    {
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
                }

                if (_options.Chart.data.datasets.Count == 1)
                {
                    _options.Chart.data.labels = dresult.Labels;
                    if (_options.Chart.type == "bar")
                    {
                        var options = _options.Chart.options as ChartJsoptionsBar;
                        options.plugins.labels.images = dresult.Images;
                    }
                    _options.Chart.data.datasets[0] = dresult.Dataset;
                    dresult.Labels = await ChartService.SortChart(_options.Chart);
                } else if (_options.Chart.data.datasets.Count > 1)
                {
                    _options.Chart.data.datasets[_options.Chart.data.datasets.Count - 1] = dresult.Dataset;

                    /*
                    if (_options.Chart.type == "bar") {
                        // TODO order
                        ChartJS sortChart = new ChartJS();
                        sortChart.options = ChartService.GetOptions(_options);
                        sortChart.data.labels = new List<string>(dresult.Labels);
                        sortChart.data.datasets.Add(dresult.Dataset);
                        var scoptions = sortChart.options as ChartJsoptionsBar;
                        scoptions.plugins.labels.images = new List<ChartJSPluginlabelsImage>(dresult.Images);
                        await ChartService.SortChart(sortChart);
                        dresult.Labels = new List<string>(sortChart.data.labels);
                        dresult.Dataset = sortChart.data.datasets.First();
                        scoptions = sortChart.options as ChartJsoptionsBar;
                        dresult.Images = new List<ChartJSPluginlabelsImage>(scoptions.plugins.labels.images);
                    }
                    */
                }

                if (dresult.CmdrInfo.Games > 0)
                    dresult.CmdrInfo.ADuration /= dresult.CmdrInfo.Games;

                dresult.CmdrInfo.AWinrate = (_options.Mode switch { 
                    "DPS" => Math.Round(dresult.CmdrInfo.Wins / dresult.CmdrInfo.Games, 2),
                    _      => Math.Round(dresult.CmdrInfo.Wins * 100 / dresult.CmdrInfo.Games, 2),
                });

                dresult.CmdrInfo.Games = dresult.CmdrInfo.GameIDs.Count();
                dresult.CmdrInfo.GameIDs = new HashSet<int>();
                _options.Cmdrinfo = new CmdrInfo(dresult.CmdrInfo);

                lock (WinrateCache)
                {
                    WinrateCache[Hash] = dresult;
                }
                _ = Computing.TryTake(out Hash);
                return (null);
            }
        }

        public static async Task<(double, int)> GetWinrate(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = await Task.Run(() => {
                if (String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                else if (String.IsNullOrEmpty(_options.Interest))
                    return from r in replays
                             from t1 in r.DSPlayer
                             where t1.RACE == cmdr
                             select new
                             {
                                 t1.WIN,
                                 r.DURATION,
                                 r.ID
                             };
                else if (!String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                else
                    return from r in replays
                           from t1 in r.DSPlayer
                           where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                           select new
                           {
                               t1.WIN,
                               r.DURATION,
                               r.ID
                           };
            });
            var resultlist = await result.ToListAsync();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static async Task<(double, int)> GetMVP(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = await Task.Run(() => {
                if (String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   r.MAXKILLSUM,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   r.MAXKILLSUM,
                                   r.DURATION,
                                   r.ID
                               };
                else if (String.IsNullOrEmpty(_options.Interest))
                    return from r in replays
                           from t1 in r.DSPlayer
                           where t1.RACE == cmdr
                           select new
                           {
                               t1.KILLSUM,
                               r.MAXKILLSUM,
                               r.DURATION,
                               r.ID
                           };
                else if (!String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   r.MAXKILLSUM,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   r.MAXKILLSUM,
                                   r.DURATION,
                                   r.ID
                               };
                else
                    return from r in replays
                           from t1 in r.DSPlayer
                           where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                           select new
                           {
                               t1.KILLSUM,
                               r.MAXKILLSUM,
                               r.DURATION,
                               r.ID
                           };
            });
            var resultlist = await result.ToListAsync();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.KILLSUM == x.MAXKILLSUM).Count();
            duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static async Task<(double, int)> GetDPS(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = await Task.Run(() => {
                if (String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   t1.ARMY,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   t1.ARMY,
                                   r.DURATION,
                                   r.ID
                               };
                else if (String.IsNullOrEmpty(_options.Interest))
                    return from r in replays
                           from t1 in r.DSPlayer
                           where t1.RACE == cmdr
                           select new
                           {
                               t1.KILLSUM,
                               t1.ARMY,
                               r.DURATION,
                               r.ID
                           };
                else if (!String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   t1.ARMY,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                               select new
                               {
                                   t1.KILLSUM,
                                   t1.ARMY,
                                   r.DURATION,
                                   r.ID
                               };
                else
                    return from r in replays
                           from t1 in r.DSPlayer
                           where t1.RACE == _options.Interest && t1.OPPRACE == cmdr
                           select new
                           {
                               t1.KILLSUM,
                               t1.ARMY,
                               r.DURATION,
                               r.ID
                           };
            });
            var resultlist = await result.ToListAsync();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Sum(s => (double)s.KILLSUM / (double)s.ARMY);
            duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins / games, 2), (int)games);
        }
        public static async Task<(double, int)> GetSynergy(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = await Task.Run(() => {
                if (!String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == _options.Interest
                               from t3 in r.DSPlayer
                               where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == _options.Interest
                               from t3 in r.DSPlayer
                               where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                else
                    return from r in replays
                           from t1 in r.DSPlayer
                           where t1.RACE == _options.Interest
                           from t3 in r.DSPlayer
                           where t3.REALPOS != t1.REALPOS && t3.TEAM == t1.TEAM && t3.RACE == cmdr
                           select new
                           {
                               t1.WIN,
                               r.DURATION,
                               r.ID
                           };
            });
            var resultlist = await result.ToListAsync();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static async Task<(double, int)> GetAntiSynergy(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            var result = await Task.Run(() => {
                if (!String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == _options.Interest
                               from t3 in r.DSPlayer
                               where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in replays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == _options.Interest
                               from t3 in r.DSPlayer
                               where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                else
                    return from r in replays
                           from t1 in r.DSPlayer
                           where t1.RACE == _options.Interest
                           from t3 in r.DSPlayer
                           where t3.TEAM != t1.TEAM && t3.RACE == cmdr
                           select new
                           {
                               t1.WIN,
                               r.DURATION,
                               r.ID
                           };
            });
            var resultlist = await result.ToListAsync();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
            cmdrInfo.GameIDs.UnionWith(resultlist.Select(s => s.ID).ToHashSet());

            cmdrInfo.Games += games;
            cmdrInfo.Wins += wins;
            cmdrInfo.ADuration += duration;
            cmdrInfo.CmdrCount.Add(new KeyValuePair<string, int>(cmdr, (int)games));

            return (Math.Round(wins * 100 / games, 2), (int)games);
        }
        public static async Task<(double, int)> GetTimeline(DSoptions _options, DSReplayContext context, IQueryable<DSReplay> replays, CmdrInfo cmdrInfo, string cmdr)
        {
            DateTime t = DateTime.ParseExact(cmdr, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var treplays = replays.Where(x => x.GAMETIME > t.AddDays(-7) && x.GAMETIME < t);

            var result = await Task.Run(() => {
                if (!String.IsNullOrEmpty(_options.Interest) && _options.Player)
                    if (String.IsNullOrEmpty(_options.Dataset))
                        return from r in treplays
                               from t1 in r.DSPlayer
                               where t1.NAME.Length == 64 && t1.RACE == _options.Interest
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                    else
                        return from r in treplays
                               from t1 in r.DSPlayer
                               where t1.NAME == _options.Dataset && t1.RACE == _options.Interest
                               select new
                               {
                                   t1.WIN,
                                   r.DURATION,
                                   r.ID
                               };
                else
                    return from r in treplays
                           from t1 in r.DSPlayer
                           where t1.RACE == _options.Interest
                           select new
                           {
                               t1.WIN,
                               r.DURATION,
                               r.ID
                           };
            });
            var resultlist = await result.ToListAsync();

            double games = 0;
            double wins = 0;
            TimeSpan duration = TimeSpan.Zero;

            games = resultlist.Count();
            wins = resultlist.Where(x => x.WIN == true).Count();
            duration = TimeSpan.FromTicks(resultlist.Sum(s => s.DURATION.Ticks));
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
