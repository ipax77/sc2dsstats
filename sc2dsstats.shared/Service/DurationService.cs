using MathNet.Numerics;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Microsoft.Scripting.Ast;
using Newtonsoft.Json;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace sc2dsstats.shared.Service
{
    public static class DurationService
    {
        public static Regex d_rx = new Regex(@"^(\d)+");
        public static DataResult GetDurations(DSoptions _options, DSReplayContext _context, IJSRuntime _jsRuntime, object lockobject)
        {
            if (String.IsNullOrEmpty(_options.Interest))
                return new DataResult();

            var replays = DBReplayFilter.Filter(_options, _context, false);

            var result = (_options.Player, _options.Dataset.Any()) switch
            {
                (true, false) => from r in replays
                                 from p in r.DSPlayer
                                 where p.RACE == _options.Interest && p.NAME.Length == 64
                                 select new
                                 {
                                     r.ID,
                                     r.DURATION,
                                     p.WIN
                                 },
                (true, true) => from r in replays
                                from p in r.DSPlayer
                                where p.RACE == _options.Interest && _options.Dataset.Contains(p.NAME)
                                select new
                                {
                                    r.ID,
                                    r.DURATION,
                                    p.WIN
                                },
                _ => from r in replays
                     from p in r.DSPlayer
                     where p.RACE == _options.Interest
                     select new
                     {
                         r.ID,
                         r.DURATION,
                         p.WIN
                     }
            };
            var lresult = result.ToList();

            if (!lresult.Any())
                return new DataResult();

            CultureInfo provider = CultureInfo.InvariantCulture;
            DataResult dresult = new DataResult();
            List<double> data = new List<double>();
            List<double> xdata = new List<double>();
            List<double> ydata = new List<double>();
            List<string> labels = new List<string>();
            Func<double, double> f = null;

            int count = lresult.Count;
            dresult.CmdrInfo.Wins = lresult.Where(x => x.WIN == true).Count();
            dresult.CmdrInfo.Games = count;
            dresult.CmdrInfo.AWinrate = Math.Round((double)dresult.CmdrInfo.Wins * 100 / dresult.CmdrInfo.Games, 2);
            dresult.CmdrInfo.ADuration = TimeSpan.FromDays(3);
            dresult.CmdrInfo.GameIDs = lresult.Select(s => s.ID).ToHashSet();


            for (int i = 0; i < DSdata.s_durations.Length; i++)
            {
                double games = 1;
                double wins = 0;
                int startd = 0;

                if (i > 0)
                {
                    Match m = d_rx.Match(DSdata.s_durations[i]);
                    if (m.Success)
                        startd = int.Parse(m.Value);
                }

                int endd = startd + 3;
                if (i == 0)
                    endd = 8;
                if (i == DSdata.s_durations.Length - 1)
                    endd = 200;

                var ilresult = lresult.Where(x => x.DURATION > startd * 60 && x.DURATION < endd * 60);
                games = ilresult.Count();
                wins = ilresult.Where(x => x.WIN == true).Count();

                data.Add(Math.Round(wins * 100 / games, 2));
                labels.Add(DSdata.s_durations[i] + " min (" + games + ")");
                xdata.Add(i);
                ydata.Add(data.Last());
            }

            if (xdata.Any() && xdata.Count > 1)
            {
                int order = 3;
                if (xdata.Count <= 3)
                    if (xdata.Count < 3)
                        order = 1;
                    else
                        order = xdata.Count - 2;
                f = Fit.PolynomialFunc(xdata.ToArray(), ydata.ToArray(), order);
            }

            dresult.Labels = labels;
            dresult.Dataset.data = data;
            dresult.fTimeline = f;
            dresult.fStartTime = DateTime.MinValue;

            ChartService.SetColor(dresult.Dataset, _options.Chart.type, _options.Interest);
            dresult.Dataset.fill = false;
            dresult.Dataset.pointRadius = 2;
            dresult.Dataset.pointHoverRadius = 10;
            dresult.Dataset.showLine = false;
            for (int i = 0; i < dresult.Labels.Count; i++)
            {
                ChartJSInterop.AddData(_jsRuntime,
                    dresult.Labels[i],
                    dresult.Dataset.data[i],
                    dresult.Dataset.backgroundColor.Last(),
                    null,
                    lockobject
                    ).GetAwaiter();
            }
            if (dresult.fTimeline != null)
            {
                ChartJSdataset dataset = new ChartJSdataset();
                dataset.label = _options.Interest + "_line";
                dataset.borderWidth = 3;
                dataset.pointRadius = 1;
                ChartService.SetColor(dataset, _options.Chart.type, _options.Interest);
                dataset.backgroundColor.Clear();
                _options.Chart.data.datasets.Add(dataset);
                ChartJSInterop.AddDataset(_jsRuntime, JsonConvert.SerializeObject(dataset, Formatting.Indented), lockobject).GetAwaiter();

                for (int i = 0; i < dresult.Labels.Count; i++)
                {
                    double fdata = dresult.fTimeline(i);

                    ChartJSInterop.AddData(_jsRuntime,
                        dresult.Labels[i],
                        fdata,
                        dresult.Dataset.backgroundColor.Last(),
                        null,
                        lockobject
                        ).GetAwaiter();
                }
            }
            return dresult;
        }
    }
}

