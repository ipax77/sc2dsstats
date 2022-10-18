using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace pax.dsstats.shared;

public record StatsResponse
{
    public StatsRequest Request { get; init; } = null!;
    public ICollection<StatsResponseItem> Items { get; init; } = null!;
    public int CountDefaultFilter { get; init; }
    public int CountNotDefaultFilter { get; init; }
    public int Bans { get; set; }
    public int AvgDuration { get; init; }
}


public record StatsResponseItem
{
    public string Label { get; init; } = null!;
    public int Matchups { get; init; }
    public int Wins { get; init; }
    [JsonIgnore]
    public long duration { get; init; }
    public int Replays { get; init; }
    public int Bans { get; set; }
    [JsonIgnore]
    public double Winrate => Matchups == 0 ? 0 : Wins * 100 / (double)Matchups;
    [JsonIgnore]
    public Commander Cmdr => Enum.TryParse(typeof(Commander), Label, out _) ? (Commander)Enum.Parse(typeof(Commander), Label) : Commander.None;
}
