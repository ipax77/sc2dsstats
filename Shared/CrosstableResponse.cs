using System;
using System.Collections.Generic;

namespace sc2dsstats._2022.Shared
{
    public class CrosstableResponse : DsResponse
    {
        new public List<CrosstableResponseItem> Items { get; set; }
    }

    public class CrosstableResponseItem : DsResponseItem
    {
        public int OldCount { get; set; }
        public int OldWins { get; set; }
        public double OldWinrate => OldCount > 0 ? Math.Round((double)OldWins * 100 / (double)OldCount, 2) : 0;
        public string Color => Winrate switch
        {
            0 => "",
            < 45 => "bg-danger",
            > 55 => "bg-success",
            _ => "bg-secondary"
        };
    }
}
