using Microsoft.JSInterop;
using Newtonsoft.Json;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.shared.Service
{
    public class ChartService
    {
        //private readonly DSstats _dsdata;
        private readonly DSDbStats _dsdata;
        private DSoptions _options;
        private readonly IJSRuntime _jsRuntime;
        private JsInteropClasses _jsIterop;
        private List<string> s_races_ordered = new List<string>(DSdata.s_races);

        //public ChartService(DSstats dsdata, IJSRuntime jsRuntime, DSoptions options)
        public ChartService(DSDbStats dsdata, IJSRuntime jsRuntime, DSoptions options)
        {
            _dsdata = dsdata;
            _jsRuntime = jsRuntime;
            _options = options;
            _jsIterop = new JsInteropClasses(_jsRuntime);
        }

        public async Task GetChartBase(bool draw = true)
        {
            ChartJS mychart = new ChartJS();
            s_races_ordered = DSdata.s_races.ToList();
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
                s_races_ordered = _s_races_cmdr_ordered;
            }

            await GetData(mychart);
            mychart.options = GetOptions();
            if (mychart.type != "line") SortChart(mychart, ref s_races_ordered);
            SetColor(mychart);
            SetCmdrPics(mychart);
            _options.Chart = mychart;

            //if (draw == true) await _jsIterop.ChartChanged(JsonSerializer.Serialize<ChartJS>(_options.Chart));
            if (draw == true) await _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
        }

        public async Task AddDataset()
        {
            ChartJS oldchart = new ChartJS();
            oldchart = _options.Chart;
            if (oldchart.data.datasets.Count() == 1 && oldchart.data.datasets[0].label == "global")
            {
                oldchart.data.datasets.RemoveAt(0);
                if (_options.Mode != "Timeline")
                    s_races_ordered = DSdata.s_races.ToList();
                await GetData(oldchart);
                //if (oldchart.type == "bar") oldchart.options.title.text = oldchart.options.title.text + " - " + _options.Interest + " vs ...";
                SortChart(oldchart, ref s_races_ordered);
                SetColor(oldchart);
                SetCmdrPics(oldchart);
                _options.Chart = oldchart;
                //await _jsIterop.ChartChanged(JsonSerializer.Serialize(_options.Chart));
                await _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
            }
            else
            {
                oldchart.data.datasets.Add(await GetData());
                SetColor(oldchart);
                _options.Chart = oldchart;
                //await _jsIterop.AddDataset(JsonSerializer.Serialize(_options.Chart.data.datasets[_options.Chart.data.datasets.Count() - 1]));
                await _jsIterop.AddDataset(JsonConvert.SerializeObject(_options.Chart.data.datasets[_options.Chart.data.datasets.Count() - 1], Formatting.Indented));
            }
        }

        public async Task RemoveDataset()
        {
            ChartJS oldchart = new ChartJS();
            oldchart = _options.Chart;
            if (oldchart.data.datasets.Count() == 1)
            {
                _options.Interest = "";
                await GetChartBase();
            }
            else
            {
                for (int i = 0; i < _options.Chart.data.datasets.Count(); i++)
                {
                    if (_options.Chart.data.datasets[i].label == _options.Interest)
                    {
                        _options.Chart.data.datasets.RemoveAt(i);
                        await _jsIterop.RemoveDataset(i);
                        break;
                    }
                }
            }
        }

        public async Task RebuildChart()
        {
            ChartJS oldchart = new ChartJS();
            oldchart = _options.Chart;
            _options.DOIT = false;

            _options.Interest = "";

            await GetChartBase(false);

            if (oldchart.data.datasets.Count() == 1 && oldchart.data.datasets[0].label == "global")
            {
                //await _jsIterop.ChartChanged(JsonSerializer.Serialize(_options.Chart));
                await _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
            }
            else
            {
                _options.Chart.data.datasets.RemoveAt(0);
                ChartJS labelChart = new ChartJS();
                foreach (var ent in oldchart.data.datasets)
                {
                    _options.Interest = ent.label;
                    _options.Chart.data.datasets.Add(await GetData(labelChart));
                }
                _options.Chart.data.labels = labelChart.data.labels;
                SortChart(_options.Chart, ref s_races_ordered);
                SetColor(_options.Chart);
                SetCmdrPics(_options.Chart);
                //await _jsIterop.ChartChanged(JsonSerializer.Serialize(_options.Chart));
                await _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
            }
            _options.DOIT = true;
        }

        public void SetColor(ChartJS mychart)
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

        public ChartJsoptions GetOptions()
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
                yAxes.scaleLabel.labelString = "% - " + _options.Startdate.ToString("yyyy-MM-dd") + " - " + _options.Enddate.ToString("yyyy-MM-dd") + " - " + IsDefaultFilter();
                if (_options.BeginAtZero == true)
                    yAxes.ticks.beginAtZero = true;

                chartoptions.scales.yAxes.Add(yAxes);
            }

            chartoptions.title.display = true;
            chartoptions.title.text = _options.Mode;
            if (_options.Player == true) chartoptions.title.text = "Player " + chartoptions.title.text;
            return chartoptions;
        }

        public string IsDefaultFilter()
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

        public async Task<ChartJSdataset> GetData(ChartJS mychart = null)
        {
            Dictionary<string, KeyValuePair<double, int>> winrate = new Dictionary<string, KeyValuePair<double, int>>();
            Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winratevs = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();

            List<string> labels = new List<string>();
            List<double> wr = new List<double>();

            string info;
            await Task.Run(() => { _dsdata.GetDynData(_options, out winrate, out winratevs, out info); });

            ChartJSdataset dataset = new ChartJSdataset();
            if (_options.Interest == "")
            {
                foreach (string race in s_races_ordered)
                {
                    if (winrate.ContainsKey(race) && winrate[race].Value > 0)
                    {
                        wr.Add(winrate[race].Key);
                        labels.Add(race + " (" + winrate[race].Value.ToString() + ")");
                    }
                    else
                    {
                        //wr.Add(0);
                        //labels.Add(race + " (0)");
                    }
                }
                dataset.label = "global";
            }
            else
            {
                foreach (string race in s_races_ordered)
                {
                    if (winratevs[_options.Interest].ContainsKey(race) && winratevs[_options.Interest][race].Value > 0)
                    {
                        wr.Add(winratevs[_options.Interest][race].Key);
                        labels.Add(race + " (" + winratevs[_options.Interest][race].Value.ToString() + ")");
                    }
                    else
                    {
                        //wr.Add(0);
                        //labels.Add(race + "(0)");
                    }
                }
                dataset.label = _options.Interest;
            }
            dataset.data = wr.ToArray();
            if (mychart != null)
            {
                mychart.data.labels = labels.ToArray();
                mychart.data.datasets.Add(dataset);
            }
            return dataset;
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

        public static ChartJS SortChart(ChartJS chart, ref List<string> s_races_ordered)
        {
            if (chart.type == "radar" || chart.type == "line") return chart;
            List<ChartJSdataset> datasets = new List<ChartJSdataset>(chart.data.datasets);
            List<ChartJSsorthelper> sortedItems = new List<ChartJSsorthelper>();

            if (datasets.Count > 0)
            {
                for (int i = 0; i < chart.data.labels.Count(); i++)
                {
                    if (chart.data.datasets[0].data.Count() > i)
                        sortedItems.Add(new ChartJSsorthelper(chart.data.labels[i], chart.data.datasets[0].data[i]));
                }
                sortedItems = sortedItems.OrderBy(o => o.WR).ToList();
                chart.data.labels = sortedItems.Select(x => x.CMDR).ToArray();
                chart.data.datasets[0].data = sortedItems.Select(x => x.WR).ToArray();

                if (datasets.Count > 1)
                {
                    for (int d = 1; d < datasets.Count(); d++)
                    {
                        List<ChartJSsorthelper> temp_sortedItems = new List<ChartJSsorthelper>();
                        //for (int i = 0; i < DSdata.s_races_cmdr.Count(); i++)
                        for (int i = 0; i < chart.data.datasets[d].data.Count(); i++)
                        {
                            temp_sortedItems.Add(new ChartJSsorthelper(DSdata.s_races[i], chart.data.datasets[d].data[i]));
                        }
                        List<ChartJSsorthelper> add_sortedItems = new List<ChartJSsorthelper>();
                        foreach (string label in chart.data.labels)
                        {
                            foreach (ChartJSsorthelper help in temp_sortedItems)
                            {
                                if (label.StartsWith(help.CMDR))
                                {
                                    add_sortedItems.Add(help);
                                }
                            }
                        }
                    }
                }
            }
            List<string> _s_races_ordered = new List<string>();
            foreach (var ent in sortedItems)
            {
                Match m = Regex.Match(ent.CMDR, @"^(\w)+");
                if (m.Success) _s_races_ordered.Add(m.Value);
            }
            s_races_ordered = _s_races_ordered;


            return chart;
        }

        public static ChartJScolorhelper GetChartColor_bak(int myi)
        {
            string temp_col;
            if (myi == 1) temp_col = "26, 94, 203";
            else if (myi == 2) temp_col = "203, 26, 59";
            else if (myi == 2) temp_col = "203, 26, 59";
            else if (myi == 3) temp_col = "47, 203, 26";
            else if (myi == 4) temp_col = "26, 203, 191";
            else if (myi == 5) temp_col = "203, 26, 177";
            else if (myi == 6) temp_col = "203, 194, 26";
            else temp_col = "72, 69, 9";

            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
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

        public static ChartJScolorhelper GetChartColor(int myi)
        {


            string temp_col;
            if (myi == 1) temp_col = "0, 0, 255";
            else if (myi == 2) temp_col = "204, 0, 0";
            else if (myi == 2) temp_col = "0, 153, 0";
            else if (myi == 3) temp_col = "204, 0, 153";
            else if (myi == 4) temp_col = "0, 204, 255";
            else if (myi == 5) temp_col = "255, 153, 0";
            else if (myi == 6) temp_col = "0, 51, 0";
            else if (myi == 7) temp_col = "0, 101, 0";
            else if (myi == 8) temp_col = "0, 151, 0";
            else if (myi == 9) temp_col = "0, 251, 0";
            else if (myi == 10) temp_col = "0, 51, 50";
            else if (myi == 11) temp_col = "0, 51, 100";
            else if (myi == 12) temp_col = "0, 51, 150";
            else if (myi == 13) temp_col = "0, 51, 200";
            else if (myi == 14) temp_col = "0, 51, 250";
            else if (myi == 15) temp_col = "50, 51, 0";
            else temp_col = "102, 51, 0";

            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.barborderColor = "rgb(255, 0, 0)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
        }

        public class ChartJScolorhelper
        {
            public string backgroundColor { get; set; }
            public string borderColor { get; set; }
            public string pointBackgroundColor { get; set; }
            public string barborderColor { get; set; }
        }

        public class ChartJSsorthelper
        {
            public string CMDR { get; set; }
            public double WR { get; set; }

            public ChartJSsorthelper(string _CMDR, double _WR)
            {
                CMDR = _CMDR;
                WR = _WR;
            }
        }

        public class JsInteropClasses
        {
            private readonly IJSRuntime _jsRuntime;

            public JsInteropClasses(IJSRuntime jsRuntime)
            {
                _jsRuntime = jsRuntime;
            }

            public async Task<string> ChartChanged(string data)
            {
                // The handleTickerChanged JavaScript method is implemented
                // in a JavaScript file, such as 'wwwroot/tickerJsInterop.js'.
                try
                {
                    return await _jsRuntime.InvokeAsync<string>("DynChart", data);
                }
                catch
                {
                    return String.Empty;
                }
            }

            public async Task<string> AddDataset(string data)
            {
                try
                {
                    return await _jsRuntime.InvokeAsync<string>("AddDynChart", data);
                }
                catch
                {
                    return String.Empty;
                }
            }

            public async Task<string> RemoveDataset(int data)
            {
                try
                {
                    return await _jsRuntime.InvokeAsync<string>("RemoveDynChart", data);
                }
                catch
                {
                    return String.Empty;
                }
            }
        }
    }

    public class ChartStateChange : INotifyPropertyChanged
    {
        private bool Update_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Dictionary<string, int> CmdrCount { get; set; } = new Dictionary<string, int>();

        public bool Update
        {
            get { return this.Update_value; }
            set
            {
                if (value != this.Update_value)
                {
                    this.Update_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
