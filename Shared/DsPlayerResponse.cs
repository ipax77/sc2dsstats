using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace sc2dsstats._2022.Shared
{
    public class DsPlayerResponse
    {
        public string Name { get; set; }
        public string Cmdr { get; set; }
        public double Army { get; set; }
        public double Kills { get; set; }
        public double Cash { get; set; }
        public int Duration { get; set; }
        public int Pos { get; set; }
        public bool Uploader { get; set; }
        public bool Leaver { get; set; }
        public List<DsPlayerBreakpointResponse> Breakpoints { get; set; }
        [NotMapped]
        [JsonIgnore]
        public int Team => Pos <= 3 ? 0 : 1;
    }

    public class DsPlayerBreakpointResponse
    {
        public string Breakpoint { get; set; }
        public int GasCount { get; set; }
        public int UpgradesSpending { get; set; }
        public List<string> Upgrades { get; set; }
        public List<DsPlayerBreakpointUnitResponse> Units { get; set; }
    }

    public class DsPlayerBreakpointUnitResponse
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public List<DsPlayerBreakpointUnitPosResponse> Positions { get; set; }
    }

    public class DsPlayerBreakpointUnitPosResponse
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
