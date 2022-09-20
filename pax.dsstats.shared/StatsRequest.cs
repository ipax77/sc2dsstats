using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.shared;

public record StatsRequest
{
    public StatsMode StatsMode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public string? Tournament { get; set; }
    public string? Round { get; set; }
    public bool Uploaders { get; set; }
}

public enum StatsMode
{
    None = 0,
    Winrate = 1,
    Timeline = 2
}
