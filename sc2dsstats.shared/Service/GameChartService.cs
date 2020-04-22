using Microsoft.JSInterop;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static sc2dsstats.shared.Service.ChartService;

namespace sc2dsstats.shared.Service
{
        public class GameChartService
        {
            private readonly IJSRuntime _jsRuntime;
            public ChartJS mychart { get; set; } = new ChartJS();

            public List<string> colorPool = new List<string>()
            {
                "0, 0, 255",
                "204, 0, 0",
                "0, 153, 0",
                "204, 0, 153",
                "0, 204, 255",
                "255, 153, 0",
                "0, 51, 0",
                "0, 101, 0",
                "0, 151, 0",
                "0, 251, 0",
                "0, 51, 50",
                "0, 51, 100",
                "0, 51, 150",
                "0, 51, 200",
                "0, 51, 250",
                "50, 51, 0",
            };

            List<string> mycolorPool;
            Regex rx_col = new Regex(@"^rgba\((\d+, \d+, \d+)");



            JsonWriterOptions jOption = new JsonWriterOptions()
            {
                Indented = true
            };

            //public GameChartService(DSdataModel dsdata, IJSRuntime jsRuntime)
            public GameChartService(IJSRuntime jsRuntime)
            {
                //_dsData = dsdata;
                _jsRuntime = jsRuntime;
                mycolorPool = new List<string>(colorPool);
            }

            public async Task<ChartJS> GetChartBase(bool draw = true)
            {
                mychart = new ChartJS();
                mychart.type = "line";
                mychart.options = GetOptions();
                mychart.options.title.text = "game details";
                mychart.options.title.fontColor = "#c9c9ff";
                mychart.options.legend.labels.fontColor = "#c9c9ff";
                if (draw == true) await ChartJSInterop.ChartChanged(_jsRuntime, JsonSerializer.Serialize(mychart));
                mycolorPool = new List<string>(colorPool);
                return mychart;
            }

            public async Task<ChartJS> AddDataset(ChartJSdataset dataset)
            {
                var col = GetRandomChartColor();
                dataset.backgroundColor.Add("rgba(0, 0, 0, 0)");
                dataset.borderColor = col.borderColor;
                dataset.pointBackgroundColor = col.pointBackgroundColor;
                mychart.data.datasets.Add(dataset);
                await ChartJSInterop.AddDataset(_jsRuntime, JsonSerializer.Serialize(dataset), new object());
                return mychart;
            }

            public async Task<ChartJS> RemoveDataset(string label)
            {
                if (mychart.data.datasets.Count() == 0) return mychart;
                int i = mychart.data.datasets.Count() - 1;
                try
                {
                    i = mychart.data.datasets.FindIndex(x => x.label == label);
                    string col = mychart.data.datasets[i].borderColor;
                    Match m = rx_col.Match(col);
                    if (m.Success)
                        mycolorPool.Add(m.Groups[1].Value.ToString());

                    mychart.data.datasets.RemoveAt(i);
                }
                catch { }
                await ChartJSInterop.RemoveDataset(_jsRuntime, i, new object());
                return mychart;
            }

            public async Task DrawChart(ChartJS chart)
            {
                mychart = chart;
                await ChartJSInterop.ChartChanged(_jsRuntime, JsonSerializer.Serialize(mychart));
            }

            public ChartJsoptions GetOptions()
            {
                ChartJsoptions chartoptions = new ChartJsoptions();
                ChartJSoptionsScalesY yAxes = new ChartJSoptionsScalesY();
                yAxes.ticks.beginAtZero = true;
                chartoptions.scales.yAxes.Add(yAxes);
                chartoptions.title.display = true;
                return chartoptions;
            }


            public ChartJScolorhelper GetRandomChartColor()
            {
                Random rnd = new Random();

                string temp_col = "50, 51, 0";

                if (mycolorPool.Count() > 0)
                {
                    int iCol = rnd.Next(0, mycolorPool.Count());
                    temp_col = mycolorPool[iCol];
                    mycolorPool.RemoveAt(iCol);
                }
                ChartJScolorhelper col = new ChartJScolorhelper();
                col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
                col.borderColor = "rgba(" + temp_col + ",1)";
                col.barborderColor = "rgb(255, 0, 0)";
                col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
                return col;
            }

        public ChartJScolorhelper GetChartColor(bool winner)
        {
            Random rnd = new Random();

            string temp_col = "255, 255, 255";

            if (winner)
                temp_col = "1, 200, 1";
            else
                temp_col = "200, 1, 1";
            

            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.barborderColor = "rgb(255, 0, 0)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
        }

        public void CreateMiddleChart(DSReplay replay, ChartJS _chart)
        {
            List<string> labels = new List<string>();
            List<double> midTeam1 = new List<double>();
            List<double> midTeam2 = new List<double>();
            TimeSpan gt = TimeSpan.FromSeconds(replay.DURATION) / 100;


            for (int i = 0; i < 100; i++) {
                TimeSpan gtint = gt * i;
                labels.Add(gtint.ToString(@"hh\:mm\:ss"));
                midTeam1.Add(replay.GetMiddle((int)(gtint.TotalSeconds * 22.4), 0) / 22.4);
                midTeam2.Add(replay.GetMiddle((int)(gtint.TotalSeconds * 22.4), 1) / 22.4);
            }

            _chart.data.labels = labels;

            ChartJSdataset dsTeam1 = new ChartJSdataset();
            ChartJSdataset dsTeam2 = new ChartJSdataset();

            dsTeam1.label = "Team1";
            dsTeam2.label = "Team2";

            var col = GetChartColor(replay.WINNER == 0);
            dsTeam1.backgroundColor.Add("rgba(0, 0, 0, 0)");
            dsTeam1.borderColor = col.borderColor;
            dsTeam1.pointBackgroundColor = col.pointBackgroundColor;

            col = GetChartColor(replay.WINNER == 1);
            dsTeam2.backgroundColor.Add("rgba(0, 0, 0, 0)");
            dsTeam2.borderColor = col.borderColor;
            dsTeam2.pointBackgroundColor = col.pointBackgroundColor;

            dsTeam1.data = midTeam1;
            dsTeam2.data = midTeam2;

            _chart.data.datasets.Add(dsTeam1);
            _chart.data.datasets.Add(dsTeam2);

            DrawChart(_chart);
        }
    }
}


