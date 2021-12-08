using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.PieChart;
using ChartJs.Blazor.RadarChart;
using sc2dsstats._2022.Shared;

namespace sc2dsstats.rlib.Services
{
    public class ChartService
    {
        public static bool AddChartDataSet(ConfigBase config, DsRequest request, DsResponse response)
        {
            return request.ChartType switch
            {
                "Bar" => AddBarDataSet(config as BarConfig, request, response, false),
                "Line" => AddLineDataSet(config as LineConfig, request, response as TimelineResponse, true),
                "Radar" => AddRadarDataSet(config as RadarConfig, request, response, true),
                "Pie" => AddPieDataSet(config as PieConfig, request, response),
                _ => false
            };
        }

        public static bool RemoveChartDataSet(ConfigBase config, DsRequest request, DsResponse response)
        {
            return request.ChartType switch
            {
                "Bar" => RemoveBarDataSet(config as BarConfig, request),
                "Line" => RemoveLineDataSet(config as LineConfig, request),
                "Radar" => RemoveRadarDataSet(config as RadarConfig, request),
                "Pie" => RemovePieDataSet(config as PieConfig, request),
                _ => false
            };
        }

        private static bool RemovePieDataSet(PieConfig config, DsRequest request)
        {
            var datasets = config.Data.Datasets.Cast<PieDataset<int>>().ToList();
            foreach (var dataset in datasets)
            {
                config.Data.Datasets.Remove(dataset);
            }
            return true;
        }

        private static bool RemoveLineDataSet(LineConfig config, DsRequest request)
        {
            var datasets = config.Data.Datasets.Cast<LineDataset<double>>().Where(x => x.Label.StartsWith(request.Interest)).ToList();
            foreach (var dataset in datasets)
            {
                config.Data.Datasets.Remove(dataset);
            }
            return true;
        }

        private static bool RemoveBarDataSet(BarConfig config, DsRequest request)
        {
            var datasets = config.Data.Datasets.Cast<BarDataset<double>>().Where(x => x.Label.StartsWith(request.Interest)).ToList();
            foreach (var dataset in datasets)
            {
                config.Data.Datasets.Remove(dataset);
            }
            return true;
        }
        private static bool RemoveRadarDataSet(RadarConfig config, DsRequest request)
        {
            var datasets = config.Data.Datasets.Cast<RadarDataset<double>>().Where(x => x.Label.StartsWith(request.Interest)).ToList();
            foreach (var dataset in datasets)
            {
                config.Data.Datasets.Remove(dataset);
            }
            return true;
        }

        private static bool AddPieDataSet(PieConfig config, DsRequest request, DsResponse response)
        {
            config.Data.Datasets.Clear();
            config.Data.Labels.Clear();
            config.Options.Title.Text = $"{request.Mode} {(request.Interest == "ALL" ? "" : $"{request.Interest} ")}- {request.Timespan} {(request.Player ? "- Uploaders" : "")}";

            List<ChartJsPluginLabelOptionsImage> Images = new List<ChartJsPluginLabelOptionsImage>();
            List<int> piedata = new List<int>();
            List<string> colors = new List<string>();

            foreach (var item in response.Items.OrderByDescending(o => o.Count))
            {
                config.Data.Labels.Add($"{item.Label} ({item.Count})");
                piedata.Add(item.Count);
                colors.Add(DSData.CMDRcolor[item.Label]);
                Images.Add(new ChartJsPluginLabelOptionsImage()
                {
                    // src = $"_content/sc2dsstats.rlib/images/btn-unit-hero-{item.Label.ToLower()}.png"
                    src = DSData.GetImageSource(item.Label)
                });

            }

            config.Options.Plugins["labels"] = new ChartJsPluginLabelOptions()
            {
                images = Images
            };
            config.Options.Plugins["datalabels"] = new ChartJsPluginDatalabelOptions();

            var piedataset = new PieDataset<int>(piedata)
            {
                BackgroundColor = colors.ToArray(),
            };

            config.Data.Datasets.Add(piedataset);

            return true;
        }

        private static bool AddBarDataSet(BarConfig config, DsRequest request, DsResponse response, bool withLables = true)
        {
            List<double> barData = new List<double>();
            List<string> borderColors = new List<string>();
            List<ChartJsPluginLabelOptionsImage> Images = new List<ChartJsPluginLabelOptionsImage>();

            if (!config.Data.Datasets.Any())
            {
                config.Data.Labels.Clear();
                foreach (var item in response.Items.OrderBy(o => o.Winrate))
                {
                    config.Data.Labels.Add($"{item.Label} ({item.Count})");
                    barData.Add(item.Count == 0 ? 0 : item.Winrate);
                    if (request.Mode != "Standard")
                    {
                        Images.Add(new ChartJsPluginLabelOptionsImage()
                        {
                            src = DSData.GetImageSource(item.Label)
                        });
                    }
                    if (request.Interest == "ALL")
                        borderColors.Add(DSData.GetColor(item.Label));
                    else
                        borderColors.Add(DSData.GetColor(response.Interest));
                }
                config.Options.Plugins["labels"] = new ChartJsPluginLabelOptions()
                {
                    images = Images
                };
                //config.Plugins.Clear();
                //config.Plugins.Add("ChartDataLabels");
                config.Options.Plugins["datalabels"] = new ChartJsPluginDatalabelOptions();
            }
            else
            {
                foreach (var label in config.Data.Labels)
                {
                    var item = response.Items.FirstOrDefault(f => label.StartsWith(f.Label));
                    if (item != null)
                    {
                        barData.Add(item.Count == 0 ? 0 : item.Winrate);
                        if (request.Interest == "ALL")
                            borderColors.Add(DSData.CMDRcolor[item.Label]);
                        else
                            borderColors.Add(DSData.CMDRcolor[response.Interest]);
                    }
                }
            }

            var pointDataset = new BarDataset<double>(barData)
            {
                Label = request.Interest,
                BorderColor = borderColors.ToArray(),
                BorderWidth = 2,
                BackgroundColor = borderColors.Select(s => s + "33").ToArray()
            };

            config.Data.Datasets.Add(pointDataset);
            return true;
        }

        private static bool AddRadarDataSet(RadarConfig config, DsRequest request, DsResponse response, bool withLables = true)
        {
            List<double> radarData = new List<double>();

            if (withLables)
                config.Data.Labels.Clear();

            foreach (var item in response.Items)
            {
                if (withLables)
                    config.Data.Labels.Add(item.Label);
                radarData.Add(item.Count == 0 ? 0 : item.Winrate);
            }

            var radarDataset = new RadarDataset<double>(radarData)
            {
                Label = response.Interest,
                BorderColor = DSData.CMDRcolor[response.Interest],
                BorderWidth = 3,
                BackgroundColor = DSData.CMDRcolor[response.Interest] + "33",
                PointRadius = 4,
                PointHoverRadius = 1,
                Fill = true
            };

            config.Data.Datasets.Add(radarDataset);
            return true;
        }

        private static bool AddLineDataSet(LineConfig config, DsRequest request, TimelineResponse response, bool withLables = true)
        {
            List<double> pointData = new List<double>();

            if (withLables)
                config.Data.Labels.Clear();

            foreach (var item in response.Items)
            {
                if (withLables)
                    config.Data.Labels.Add(item.Label);
                pointData.Add(item.Count == 0 ? 0 : Math.Round((double)item.Wins * 100.0 / (double)item.Count, 2));
            }

            var pointDataset = new LineDataset<double>(pointData)
            {
                Label = response.Interest,
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
            return true;
        }

        public static ConfigBase GetChartConfig(DsRequest request, DsResponse response, bool playerStats = false, string canvasId = null)
        {
            var config = request.ChartType switch
            {
                "Bar" => GetChartBarConfig(request, response as DsResponse, playerStats),
                "Line" => GetChartLineConfig(request, response as TimelineResponse, playerStats),
                "Radar" => GetChartRadarConfig(request, response, playerStats),
                "Pie" => GetPieConfig(request, response, playerStats),
                _ => null
            };
            if (canvasId != null)
            {
                config.CanvasId = canvasId;
            }
            return config;
        }

        public static ConfigBase GetPieConfig(DsRequest request, DsResponse response, bool playerStats)
        {
            var config = GetPieConfig(request);
            config.Options.Title.Text = $"{request.Mode} - {request.Timespan} {(request.Player ? $"- {(playerStats ? "Player" : "Uploaders")}" : "")}";
            AddPieDataSet(config, request, response);
            return config;
        }

        public static ConfigBase GetChartRadarConfig(DsRequest request, DsResponse response, bool playerStats)
        {
            var config = GetRadarConfig(request);
            config.Options.Title.Text = $"{request.Mode} - {request.Timespan} {(request.Player ? $"- {(playerStats ? "Player" : "Uploaders")}" : "")}";
            config.Options.Plugins["datalabels"] = new ChartJsPluginDatalabelOptions()
            {
                display = false
            };
            AddRadarDataSet(config, request, response);

            return config;
        }

        public static ConfigBase GetChartLineConfig(DsRequest request, TimelineResponse response, bool playerStats)
        {
            var config = GetLineConfig(request);
            config.Options.Title.Text = $"{request.Mode} - {request.Timespan} {(request.Player ? $"- {(playerStats ? "Player" : "Uploaders")}" : "")}";
            config.Options.Plugins["datalabels"] = new ChartJsPluginDatalabelOptions()
            {
                display = false
            };
            AddLineDataSet(config, request, response);
            return config;
        }

        public static ConfigBase GetChartBarConfig(DsRequest request, DsResponse response, bool playerStats)
        {
            var config = GetBarConfig(request);
            config.Options.Title.Text = $"{request.Mode} - {request.Timespan} {(request.Player ? $"- {(playerStats ? "Player" : "Uploaders")}" : "")}";

            if (request.Filter != null && !request.Filter.isDefault)
            {
                config.Options.Title.Text = $"{request.Mode} - {(request.Timespan == "Custom" ? $"{request.StartTime.ToString("yyyy-MM-dd")} - {request.EndTime.ToString("yyyy-MM-dd")}" : request.Timespan)} {(request.Player ? "- Uploaders" : "")}";
                config.Options.Scales.YAxes.First().ScaleLabel.LabelString = $"Winrate % - Custom Filter";
            }

            AddBarDataSet(config, request, response);

            return config;
        }

        public static LineConfig GetLineConfig(DsRequest request)
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
                                    BeginAtZero = request.BeginAtZero,
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

        private static BarConfig GetBarConfig(DsRequest request)
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
                                    LabelString = "Commander (Matchups)",
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
                                    BeginAtZero = request.BeginAtZero,
                                    FontColor = "#919191"
                                },
                                ScaleLabel = new ScaleLabel()
                                {
                                    LabelString = "Winrate % - Default Filter",
                                    Display = true
                                }
                            }
                        }
                    }
                }
            };
        }

        private static RadarConfig GetRadarConfig(DsRequest request)
        {
            return new RadarConfig()
            {
                Options = new RadarOptions
                {
                    Legend = new Legend()
                    {
                        Labels = new LegendLabels()
                        {
                            FontColor = "#f2f2f2",
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
                        Text = "Synergy",
                        FontColor = "#f2f2f2"
                    },
                    Scale = new LinearRadialAxis
                    {
                        AngleLines = new AngleLines
                        {
                            Color = "#f2f2f233"
                        },
                        GridLines = new GridLines
                        {
                            Color = "#f2f2f233"
                        },
                        PointLabels = new PointLabels
                        {
                            FontColor = "#f2f2f2",
                            FontSize = 12
                        },
                        Ticks = new LinearRadialTicks
                        {
                            // BackdropColor = 
                            BeginAtZero = request.BeginAtZero
                        }
                    }
                }
            };
        }

        private static PieConfig GetPieConfig(DsRequest request)
        {
            return new PieConfig()
            {
                Options = new PieOptions
                {
                    Legend = new Legend()
                    {
                        Position = Position.Right,
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
                        Text = "Commanders Played",
                        FontColor = "#f2f2f2"
                    },
                }
            };
        }
    }
}
