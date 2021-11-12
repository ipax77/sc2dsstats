using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.LineChart;
using sc2dsstats._2022.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats._2022.Client.Services
{
    public class ChartService
    {
        public static ConfigBase GetChartConfig(TimelineResponse response)
        {
            var config = GetLineConfig();
            config.Options.Title.Text = response.Interest + (response.Versus == "ALL" ? "" : " vs " + response.Versus);

            List<double> pointData = new List<double>();

            foreach (var item in response.Items)
            {
                config.Data.Labels.Add(item.Label);
                pointData.Add(item.Count == 0 ? 0 : Math.Round((double)item.Wins * 100.0 / (double)item.Count, 2));
            }

            var pointDataset = new LineDataset<double>(pointData)
            {
                Label = "",
                BorderColor = DSData.CMDRcolor[response.Interest],
                BorderWidth = 1,
                PointRadius = 2,
                PointHoverRadius = 10,
                ShowLine = false,
                Fill = false
            };

            var timelineDataset = new LineDataset<double>(response.SmaData)
            {
                Label = response.Interest,
                BorderColor = DSData.CMDRcolor[response.Interest],
                BorderWidth = 3,
                PointRadius = 1,
                PointHoverRadius = 1,
                ShowLine = true,
                Fill = false
            };

            config.Data.Datasets.Add(pointDataset);
            config.Data.Datasets.Add(timelineDataset);

            return config;
        }

        public static ConfigBase GetChartConfig(DsResponse response)
        {
            var config = GetBarConfig();
            config.Options.Title.Text = response.Interest;

            List<double> pointData = new List<double>();

            foreach (var item in response.Items)
            {
                config.Data.Labels.Add(item.Label);
                pointData.Add(item.Count == 0 ? 0 : Math.Round((double)item.Wins * 100.0 / (double)item.Count, 2));
            }

            var pointDataset = new BarDataset<double>(pointData)
            {
                Label = "global",
                BorderColor = DSData.CMDRcolor[response.Interest],
                BorderWidth = 2,
            };

            config.Data.Datasets.Add(pointDataset);

            return config;
        }

        public static LineConfig GetLineConfig()
        {
            LineConfig _config = new LineConfig()
            {
                Options = new LineOptions
                {
                    Responsive = true,
                    Title = new OptionsTitle
                    {
                        Display = true,
                        Padding = 0,
                        FontSize = 20,
                        FontColor = "#f2f2f2",
                        Text = "Loading ..."
                    },
                    Scales = new Scales
                    {
                        XAxes = new List<CartesianAxis>
                        {
                            new CategoryAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = "Dates"
                                },
                                Ticks = new CategoryTicks()
                                {
                                    FontColor = "#919191"
                                }
                            }
                        },
                        YAxes = new List<CartesianAxis>
                        {
                            new LinearCartesianAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = "Winrate"
                                },
                                Ticks = new LinearCartesianTicks()
                                {
                                    BeginAtZero = false,
                                    FontColor = "#919191"
                                }
                            }
                        }
                    }
                }
            };
            //_config.Options.Plugins.Add("datalabels", new ChartJsPluginDatalabelOptions() { display = false });
            //_config.Options.Plugins.Add("labels", new ChartJsPluginLabelOptions());
            return _config;
        }

        private static BarConfig GetBarConfig()
        {
            return new BarConfig()
            {
                Options = new BarOptions
                {
                    Legend = new Legend()
                    {
                        Labels = new LegendLabels()
                        {
                            FontColor = "#919191",
                        }
                    },
                    Responsive = true,
                    Tooltips = new Tooltips()
                    {
                        Enabled = true,
                        Intersect = false
                    },
                    Hover = new Hover()
                    {
                        Intersect = true
                    },
                    Title = new OptionsTitle
                    {
                        Display = true,
                        Padding = 5,
                        FontSize = 20,
                        Text = "Winrate",
                        FontColor = "#f2f2f2"
                    },
                    Scales = new BarScales
                    {
                        XAxes = new List<CartesianAxis>()
                        {
                            new BarCategoryAxis
                            {
                                Ticks = new CategoryTicks()
                                {
                                    FontColor = "#919191"
                                },
                                ScaleLabel = new ScaleLabel()
                                {
                                    LabelString = "Commander Games",
                                    Display = true
                                },
                            }
                        },
                        YAxes = new List<CartesianAxis>
                        {
                            new LinearCartesianAxis
                            {
                                Ticks = new LinearCartesianTicks()
                                {
                                    BeginAtZero = false,
                                    FontColor = "#919191"
                                },
                                ScaleLabel = new ScaleLabel()
                                {
                                    LabelString = "Default Filter",
                                    Display = true
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
