using System;
using System.Collections.Generic;

namespace sc2dsstats._2022.Shared
{
    public class DsBuildResponse
    {
        public string Interest { get; set; }
        public string Versus { get; set; }
        public int Count { get; set; }
        public int Duration { get; set; }
        public int Gas { get; set; }
        public int Wins { get; set; }
        public int Upgrades { get; set; }
        public List<DsBuildResponseBreakpoint> Breakpoints { get; set; }
        public List<DsBuildResponseReplay> Replays { get; set; }
    }

    public class DsBuildResponseBreakpoint
    {
        public string Breakpoint { get; set; }
        public int Count { get; set; }
        public int Duration { get; set; }
        public int Gas { get; set; }
        public int Wins { get; set; }
        public int Upgrades { get; set; }
        public List<DsBuildResponseBreakpointUnit> Units { get; set; }

    }

    public class DsBuildResponseBreakpointUnit
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class DsBuildResponseReplay
    {
        public string Hash { get; set; }
        public DateTime Gametime { get; set; }
    }
}
