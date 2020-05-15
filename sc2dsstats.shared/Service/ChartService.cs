using Microsoft.JSInterop;
using Newtonsoft.Json;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.shared.Service
{
    public class ChartService
    {
        private IJSRuntime _jsRuntime;
        private DSReplayContext _context;
        private DBService _db;

        public ChartService(IJSRuntime jsRuntime, DSReplayContext context, DBService db)
        {
            _jsRuntime = jsRuntime;
            _context = context;
            _db = db;
        }

        public async Task ChartHandler(DSoptions _options, string thisHandle = null)
        {
            await GetChartBase(_options);

            // rebuild chart

            // add dataset

            // remove dataset

            // change chart options (player, beginatzero)
        }

        public async Task Init(DSoptions _options)
        {
            _options.Chart = await GetChartBase(_options, false);
            await ChartJSInterop.ChartChanged(_jsRuntime, JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
        }

        public async Task<ChartJS> GetChartBase(DSoptions _options, bool doit = true)
        {
            ChartJS mychart = new ChartJS();
            

            List<string> GameModes = _options.Gamemodes.Where(x => x.Value == true).Select(s => s.Key).ToList();

            if (GameModes.Contains("GameModeStandard") || GameModes.Contains("GameModeGear") || GameModes.Contains("GameModeSabotage") || GameModes.Contains("GameModeSwitch"))
                mychart.s_races_ordered = DSdata.s_races.ToList();
            else
                mychart.s_races_ordered = DSdata.s_races_cmdr.ToList();

            mychart.type = "bar";


            if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy")
                mychart.type = "radar";
            else if (_options.Mode == "Timeline")
            {
                mychart.type = "line";
                List<string> _s_races_cmdr_ordered = new List<string>();
                DateTime startdate = _options.Startdate;
                if (startdate == DateTime.MinValue)
                    startdate = new DateTime(2018, 1, 1);
                DateTime enddate = _options.Enddate;
                if (enddate == DateTime.MinValue)
                    enddate = DateTime.Now.AddDays(1);
                DateTime breakpoint = startdate;
                while (DateTime.Compare(breakpoint, enddate) < 0)
                {
                    breakpoint = breakpoint.AddDays(1);
                    _s_races_cmdr_ordered.Add(breakpoint.ToString("yyyy-MM-dd"));
                }
                _s_races_cmdr_ordered.RemoveAt(_s_races_cmdr_ordered.Count() - 1);
                mychart.s_races_ordered = _s_races_cmdr_ordered;
            }
            mychart.options = GetOptions(_options);
            mychart.s_races = new List<string>(mychart.s_races_ordered);
            if (doit)
            {
                ChartJSdataset dataset = new ChartJSdataset();

                if (_options.Mode == "Timeline")
                {
                    dataset.fill = false;
                    dataset.pointRadius = 2;
                    dataset.pointHoverRadius = 10;
                    dataset.showLine = false;
                }

                if (String.IsNullOrEmpty(_options.Interest))
                {
                    dataset.label = "global";
                    if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy" || _options.Mode == "Timeline")
                    {
                        _options.Interest = "Abathur";
                        dataset.label = _options.Interest;
                        _options.CmdrsChecked["Abathur"] = true;
                    }
                }
                else
                    dataset.label = _options.Interest;

                SetColor(dataset, mychart.type, _options.Interest);
                dataset.backgroundColor.Clear();
                mychart.data.datasets.Add(dataset);
                _options.Chart = mychart;
                await ChartJSInterop.ChartChanged(_jsRuntime, JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));


                DataResult dresult = await DataService.GetData(_options, _context, _jsRuntime, _db.lockobject);
                
                if (dresult != null)
                {
                    _options.Chart.data.labels = new List<string>(dresult.Labels);
                    _options.Chart.data.datasets[0] = dresult.Dataset.DeepCopy();
                    if (_options.Chart.type == "bar")
                    {
                        var options = _options.Chart.options as ChartJsoptionsBar;
                        options.plugins.labels.images = dresult.Images;
                    }
                    _options.Cmdrinfo = dresult.CmdrInfo;
                    SetCmdrPics(_options.Chart);
                    await ChartJSInterop.ChartChanged(_jsRuntime, JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
                    if (dresult.fTimeline != null)
                        await DrawTimeline(_options, dresult, _db.lockobject);
                }
                await SortChart(_options.Chart);
            }
            return mychart;
        }

        public async Task ChangeOption(DSoptions _options)
        {
            if (_options.Chart.type == "bar")
            {
                ChartJsoptionsBar chartoptions = _options.Chart.options as ChartJsoptionsBar;
                chartoptions.scales.yAxes.First().ticks.beginAtZero = _options.BeginAtZero;
                var schartoptions = JsonConvert.SerializeObject(chartoptions);
                await ChartJSInterop.ChangeOptions(_jsRuntime, schartoptions);
            }
        }

        public async Task DrawTimeline(DSoptions _options, DataResult dresult, object lockobject)
        {
            ChartJSdataset dataset = new ChartJSdataset();
            dataset.label = _options.Interest + "_line";
            dataset.borderWidth = 3;
            dataset.pointRadius = 1;
            ChartService.SetColor(dataset, _options.Chart.type, _options.Interest);
            dataset.backgroundColor.Clear();
            _options.Chart.data.datasets.Add(dataset);
            ChartJSInterop.AddDataset(_jsRuntime, JsonConvert.SerializeObject(dataset, Formatting.Indented), lockobject).GetAwaiter();

            for (int i = 0; i < _options.Chart.data.labels.Count; i++)
            {
                ChartJSInterop.AddData(_jsRuntime,
                    _options.Chart.data.labels[i],
                    dresult.fTimeline(i),
                    dresult.Dataset.backgroundColor.Last(),
                    null,
                    lockobject
                    ).GetAwaiter();
            }
        }

        public async Task AddDataset(DSoptions _options, object lockobject)
        {
            ChartJSdataset dataset = new ChartJSdataset();
            if (String.IsNullOrEmpty(_options.Interest))
                dataset.label = "global";
            else
                dataset.label = _options.Interest;

            if (_options.Mode == "Timeline")
            {
                dataset.fill = false;
                dataset.pointRadius = 2;
                dataset.pointHoverRadius = 10;
                dataset.showLine = false;
            }

            SetColor(dataset, _options.Chart.type, _options.Interest);
            dataset.backgroundColor.Clear();
            _options.Chart.data.datasets.Add(dataset);
            await ChartJSInterop.AddDataset(_jsRuntime, JsonConvert.SerializeObject(dataset, Formatting.Indented), lockobject);
            DataResult dresult = await DataService.GetData(_options, _context, _jsRuntime, _db.lockobject);
            if (dresult != null)
            {
                foreach (string l in _options.Chart.data.labels)
                {
                    double wr = 0;
                    string bcolor = "rgba(0, 0, 0, 0)";

                    int ssend = 4;
                    if (_options.Chart.type == "line")
                        ssend = 10;
                    string label = dresult.Labels.FirstOrDefault(s => s.Substring(0, ssend) == l.Substring(0, ssend));
                    if (!String.IsNullOrEmpty(label))
                    {
                        int pos = dresult.Labels.FindIndex(i => i == label);
                        _options.Chart.data.datasets.Last().data.Add(dresult.Dataset.data[pos]);
                        if (_options.Chart.type == "line")
                            _options.Chart.data.datasets.Last().backgroundColor.Add(dresult.Dataset.backgroundColor.Last());
                        else
                            _options.Chart.data.datasets.Last().backgroundColor.Add(dresult.Dataset.backgroundColor[pos]);
                        wr = dresult.Dataset.data[pos];
                        if (_options.Chart.type == "line")
                            bcolor = dresult.Dataset.backgroundColor.Last();
                        else
                            bcolor = dresult.Dataset.backgroundColor[pos];
                    }
                    else
                    {
                        _options.Chart.data.datasets.Last().data.Add(0);
                        _options.Chart.data.datasets.Last().backgroundColor.Add("rgba(0, 0, 0, 0)");
                    }
                    ChartJSInterop.AddData(_jsRuntime,
                                            "",
                                            wr,
                                            bcolor,
                                            "",
                                            lockobject
                                            );
                }
                _options.Cmdrinfo = dresult.CmdrInfo;
                if (dresult.fTimeline != null)
                    DrawTimeline(_options, dresult, lockobject);
            }
        }

        public async Task RemoveDataset(DSoptions _options, string cmdr, object lockobject)
        {
            foreach (ChartJSdataset dataset in _options.Chart.data.datasets.Where(x => x.label.StartsWith(cmdr)).ToArray()) {
                int pos = _options.Chart.data.datasets.FindIndex(i => i == dataset);
                if (pos >= 0)
                {
                    _options.Chart.data.datasets.RemoveAt(pos);
                    await ChartJSInterop.RemoveDataset(_jsRuntime, pos, lockobject);
                }
            }
            //TODO: CmdrInfo info for remaining dataset
            //TODO?: Check labels and sort 
            //await SortChart(_options.Chart);
        }

        public ChartJsoptions GetOptions(DSoptions _options)
        {
            ChartJsoptions chartoptions = new ChartJsoptions();


            if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy")
            {
                ChartJsoptionsradar radaroptions = new ChartJsoptionsradar();
                radaroptions.title.text = _options.Mode + " " + _options.Time;
                radaroptions.legend.position = "bottom";
                if (_options.Player == true) radaroptions.title.text = "Player " + radaroptions.title.text;
                chartoptions = radaroptions;

            }
            else
            {
                ChartJsoptionsBar baroptions = new ChartJsoptionsBar();
                chartoptions = baroptions;

                ChartJSoptionsScalesY yAxes = new ChartJSoptionsScalesY();
                string startdate = _options.Startdate.ToString("yyyy-MM-dd");
                string enddate = _options.Enddate.ToString("yyyy-MM-dd");
                if (_options.Enddate == DateTime.MinValue)
                    enddate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                yAxes.scaleLabel.labelString = "% - " + startdate + " - " + enddate + " - " + IsDefaultFilter(_options);
                if (_options.BeginAtZero == true)
                    yAxes.ticks.beginAtZero = true;

                if (_options.Mode == "Timeline")
                    baroptions.elements.point = new ChartJSoptionsElementsPoint();

                chartoptions.scales.yAxes.Add(yAxes);
            }

            chartoptions.title.display = true;
            chartoptions.title.text = _options.Mode + " " + _options.Time;
            if (_options.Player == true) chartoptions.title.text = "Player " + chartoptions.title.text;
            return chartoptions;
        }

        public void SetCmdrPics(ChartJS chart)
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

        public async Task<List<string>> SortChart(ChartJS chart, bool dry = false)
        {
            if (chart.type != "bar") return chart.data.labels;
            if (chart.data.datasets.Count == 0 || chart.data.datasets.Count > 1) return chart.data.labels;

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
                dataset.borderColor = dcolor.barborderColor;
                dataset.borderWidth = 1;
            }
            else if (charttype == "line")
            {
                dataset.backgroundColor.Add("rgba(0, 0, 0, 0)");
                dataset.borderColor = dcolor.borderColor;
                dataset.pointBackgroundColor = dcolor.pointBackgroundColor;
            }
            else if (charttype == "radar")
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
            string cmdr_col = "#0000ff";
            if (DSdata.CMDRcolor.ContainsKey(cmdr))
                cmdr_col = DSdata.CMDRcolor[cmdr];
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
