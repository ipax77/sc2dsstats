using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace sc2dsstats._2022.Shared
{
    public class DsResponse
    {
        public int Count { get; set; }
        public string Interest { get; set; }
        public int AvgDuration { get; set; }
        public double AvgWinrate { get; set; }
        public List<DsResponseItem> Items { get; set; }
        public DsCountResponse CountResponse { get; set; }
    }

    public class DsResponseItem
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public int Wins { get; set; }
        [JsonIgnore]
        public long duration { get; set; }
        public int Replays { get; set; }
        public double Winrate => Count > 0 ? Math.Round((double)Wins * 100 / (double)Count, 2) : 0;
    }

    public class LeaverInfo
    {

    }
}
