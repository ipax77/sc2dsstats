using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.shared;

public record MmrDevDto
{
    public double Mmr { get; init; }
    public int Count { get; set; }
}
