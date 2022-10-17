using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.shared;

public record BuildRequest
{
    public List<string> PlayerNames { get; set; } = new();
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; } = DateTime.Today;
}
