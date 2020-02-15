using Microsoft.JSInterop;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.shared.Service
{
    public class ChartService
    {
        private static IJSRuntime _jsRuntime;

        public ChartService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task ChartHandler(DSoptions _options, string thisHandle = null)
        {
            await GetChartBase( _options);

            // rebuild chart

            // add dataset

            // remove dataset

            // change chart options (player, beginatzero)
        }

        public static async Task Init(DSoptions _options)
        {
            _options.Chart = await GetChartBase(_options, false);
            await ChartJSInterop.ChartChanged(_jsRuntime, JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
        }

        public static async Task<ChartJS> GetChartBase(DSoptions _options, bool doit = true)
        {
            ChartJS mychart = new ChartJS();

            List<string> GameModes = _options.Gamemodes.Where(x => x.Value == true).Select(s => s.Key).ToList();

            if (GameModes.Contains("GameModeStandard") || GameModes.Contains("GameModeGear") || GameModes.Contains("GameModeSabotage") ||GameModes.Contains("GameModeSwitch"))
                mychart.s_races_ordered = DSdata.s_races.ToList();
            else
                mychart.s_races_ordered = DSdata.s_races_cmdr.ToList();

            mychart.type = "bar";
            //mychart.type = "horizontalBar";
            if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy")
                mychart.type = "radar";
            else if (_options.Mode == "Timeline")
            {
                mychart.type = "line";
                List<string> _s_races_cmdr_ordered = new List<string>();
                DateTime startdate = _options.Startdate;
                DateTime enddate = _options.Enddate;
                DateTime breakpoint = startdate;
                while (DateTime.Compare(breakpoint, enddate) < 0)
                {
                    breakpoint = breakpoint.AddDays(7);
                    _s_races_cmdr_ordered.Add(breakpoint.ToString("yyyy-MM-dd"));
                }
                _s_races_cmdr_ordered.RemoveAt(_s_races_cmdr_ordered.Count() - 1);
                mychart.s_races_ordered = _s_races_cmdr_ordered;
            }
            mychart.options = GetOptions(_options);
            
            if (doit) {
                _options.Chart = mychart;
                DataResult dresult = await DataService.GetData(_options, _jsRuntime);
                if (dresult != null)
                {
                    _options.Chart.data.labels = new List<string>(dresult.Labels);
                    _options.Chart.data.datasets.Add(dresult.Dataset);
                    _options.Cmdrinfo = dresult.CmdrInfo;
                    SetCmdrPics(_options.Chart);
                    await ChartJSInterop.ChartChanged(_jsRuntime, JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
                }
            }
            return mychart;
        }

        public static async Task ChangeOption(DSoptions _options)
        {
            if (_options.Chart.type == "bar")
            {
                ChartJsoptionsBar chartoptions = _options.Chart.options as ChartJsoptionsBar;
                chartoptions.scales.yAxes.First().ticks.beginAtZero = _options.BeginAtZero;
                var schartoptions = JsonConvert.SerializeObject(chartoptions);
                await ChartJSInterop.ChangeOptions(_jsRuntime, schartoptions);
            }
        }

        public static async Task AddDataset(DSoptions _options, object lockobject)
        {
            DataResult dresult = await DataService.GetData(_options, _jsRuntime);
            if (dresult != null)
            {
                _options.Chart.data.datasets.Add(dresult.Dataset);
                _options.Cmdrinfo = dresult.CmdrInfo;
                //SetCmdrPics(_options.Chart);
                await ChartJSInterop.AddDataset(_jsRuntime, JsonConvert.SerializeObject(dresult.Dataset, Formatting.Indented), lockobject);
            }
        }

        public static async Task RemoveDataset(DSoptions _options, string cmdr, object lockobject)
        {
            for (int i = 0; i < _options.Chart.data.datasets.Count; i++)
            {
                if (_options.Chart.data.datasets[i].label == cmdr)
                {
                    _options.Chart.data.datasets.RemoveAt(i);
                    await ChartJSInterop.RemoveDataset(_jsRuntime, i, lockobject);
                    break;
                }
            }
        }

        public static ChartJsoptions GetOptions(DSoptions _options)
        {
            ChartJsoptions chartoptions = new ChartJsoptions();


            if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy")
            {
                ChartJsoptionsradar radaroptions = new ChartJsoptionsradar();
                radaroptions.title.text = _options.Mode;
                radaroptions.legend.position = "bottom";
                if (_options.Player == true) radaroptions.title.text = "Player " + radaroptions.title.text;
                chartoptions = radaroptions;

            }
            else
            {
                ChartJsoptionsBar baroptions = new ChartJsoptionsBar();
                chartoptions = baroptions;

                ChartJSoptionsScalesY yAxes = new ChartJSoptionsScalesY();
                yAxes.scaleLabel.labelString = "% - " + _options.Startdate.ToString("yyyy-MM-dd") + " - " + _options.Enddate.ToString("yyyy-MM-dd") + " - " + IsDefaultFilter(_options);
                if (_options.BeginAtZero == true)
                    yAxes.ticks.beginAtZero = true;

                chartoptions.scales.yAxes.Add(yAxes);
            }

            chartoptions.title.display = true;
            chartoptions.title.text = _options.Mode;
            if (_options.Player == true) chartoptions.title.text = "Player " + chartoptions.title.text;
            return chartoptions;
        }

        public static void SetCmdrPics(ChartJS chart)
        {
            if (chart.type != "bar") return;

            List<ChartJSPluginlabelsImage> images = new List<ChartJSPluginlabelsImage>();
            foreach (string lcmdr in chart.data.labels)
            {
                foreach (string cmdr in DSdata.s_races)
                {
                    if (lcmdr.StartsWith(cmdr))
                    {
                        ChartJSPluginlabelsImage myimage = new ChartJSPluginlabelsImage();
                        //myimage.src = "images/btn-unit-hero-" + cmdr.ToLower() + ".png";
                        myimage.src = "_content/sc2dsstats.shared/images/btn-unit-hero-" + cmdr.ToLower() + ".png";
                        images.Add(myimage);
                    }
                }
            }
            ChartJsoptionsBar opt = new ChartJsoptionsBar();
            opt = chart.options as ChartJsoptionsBar;
            opt.plugins.labels.images = images;
            chart.options = opt;

        }

        public static async Task<List<string>> SortChart(ChartJS chart, bool dry = false)
        {
            if (chart.type != "bar") return chart.data.labels;
            if (chart.data.datasets.Count > 1) return chart.data.labels;

            Dictionary<int, ChartJSsorthelper> sortMe = new Dictionary<int, ChartJSsorthelper>();
            var opt = chart.options as ChartJsoptionsBar;

            for (int i = 0; i < chart.data.labels.Count; i++)
                sortMe[i] = new ChartJSsorthelper(chart.data.labels[i], chart.data.datasets[0].data[i], opt.plugins.labels.images[i], chart.data.datasets[0].backgroundColor[i], chart.s_races_ordered[i]);

            var sortedMe = sortMe.Values.OrderBy(o => o.Winrate);

            chart.data.labels = sortedMe.Select(s => s.Label).ToList();
            chart.data.datasets[0].data = sortedMe.Select(s => s.Winrate).ToList();
            opt.plugins.labels.images = sortedMe.Select(s => s.Image).ToList();
            chart.data.datasets[0].backgroundColor = sortedMe.Select(s => s.Color).ToList();
            chart.s_races_ordered = sortedMe.Select(s => s.s_race).ToList();

            string labels = JsonConvert.SerializeObject(chart.data.labels);
            string winrates = JsonConvert.SerializeObject(chart.data.datasets[0].data);
            string images = JsonConvert.SerializeObject(opt.plugins.labels.images);
            string colors = JsonConvert.SerializeObject(chart.data.datasets[0].backgroundColor);

            if (dry == false)
                await ChartJSInterop.SortChart(_jsRuntime, labels, winrates, images, colors);
            return chart.data.labels;
        }

        public static void SetColor(ChartJSdataset dataset, string charttype, string cmdr)
        {
            var dcolor = GetChartColorFromLabel(cmdr);

            if (charttype == "bar")
            {
                dataset.backgroundColor.Add(dcolor.backgroundColor);
                dataset.borderColor = dcolor.borderColor;
                dataset.borderWidth = 1;
            } else if (charttype == "line")
            {
                dataset.backgroundColor.Add("rgba(0, 0, 0, 0)");
                dataset.borderColor = dcolor.borderColor;
                dataset.pointBackgroundColor = dcolor.pointBackgroundColor;
            } else if (charttype == "radar")
            {
                dataset.backgroundColor.Add(dcolor.pointBackgroundColor);
                dataset.borderColor = dcolor.borderColor;
                dataset.pointBackgroundColor = dcolor.pointBackgroundColor;
            }
        }

        public static void SetColor(ChartJS mychart)
        {
            int i = 0;
            foreach (var ent in mychart.data.datasets)
            {
                i++;
                var col = GetChartColorFromLabel(ent.label);
                if (mychart.type == "bar")
                {
                    if (ent.label == "global")
                    {
                        foreach (var cmdr in mychart.data.labels)
                        {
                            Match m = Regex.Match(cmdr, @"^(\w)+");
                            if (m.Success)
                            {
                                var col_global = GetChartColorFromLabel(m.Value);
                                ent.backgroundColor.Add(col_global.backgroundColor);
                            }
                        }
                    }
                    else
                    {
                        ent.backgroundColor.Add(col.backgroundColor);
                    }
                    ent.borderColor = col.barborderColor;
                    ent.borderWidth = 1;
                }
                else if (mychart.type == "line")
                {
                    ent.backgroundColor.Add("rgba(0, 0, 0, 0)");
                    ent.borderColor = col.borderColor;
                    ent.pointBackgroundColor = col.pointBackgroundColor;
                }
                else if (mychart.type == "radar")
                {
                    ent.backgroundColor.Add(col.pointBackgroundColor);
                    ent.borderColor = col.borderColor;
                    ent.pointBackgroundColor = col.pointBackgroundColor;
                }
            }
        }

        public static ChartJScolorhelper GetChartColorFromLabel(string cmdr)
        {
            string cmdr_col = DSdata.CMDRcolor[cmdr];
            Color color = ColorTranslator.FromHtml(cmdr_col);
            string temp_col = color.R + ", " + color.G + ", " + color.B;
            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.barborderColor = "rgb(255, 0, 0)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
        }

        public static string IsDefaultFilter(DSoptions _options)
        {
            DSoptions defoptions = new DSoptions();

            if (_options.Duration == defoptions.Duration
                && _options.Army == defoptions.Army
                && _options.Income == defoptions.Income
                && _options.Leaver == defoptions.Leaver
                && _options.Kills == defoptions.Kills
                && _options.PlayerCount == defoptions.PlayerCount
            )
                return "default Filter";
            else
                return "custom Filter";
        }

        public class ChartJSsorthelper
        {
            public string Label { get; set; }
            public string s_race { get; set; }
            public double Winrate { get; set; }
            public ChartJSPluginlabelsImage Image { get; set; }
            public string Color { get; set; }

            public ChartJSsorthelper(string label, double winrate, ChartJSPluginlabelsImage image, string color, string srace)
            {
                Label = label;
                Winrate = winrate;
                Image = image;
                Color = color;
                s_race = srace;
            }
        }

        public class ChartJScolorhelper
        {
            public string backgroundColor { get; set; }
            public string borderColor { get; set; }
            public string pointBackgroundColor { get; set; }
            public string barborderColor { get; set; }
        }

    }
}
