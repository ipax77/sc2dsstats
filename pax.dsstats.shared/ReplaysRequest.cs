using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.shared;

public record ReplaysRequest
{
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "GameTime" } };
    public DateTime StartTime { get; set; } = new DateTime(2022, 2, 1);
    public DateTime? EndTime { get; set; } = null;
    public int Skip { get; set; }
    public int Take { get; set; }
    public string? Tournament { get; set; }
    public string? SearchString { get; set; }
    public string? ReplayHash { get; set; }
}

public record Order
{
    public string Property { get; set; } = "";
    public bool Ascending { get; set; }
}