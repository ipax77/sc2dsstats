using System;

namespace sc2dsstats._2022.Shared
{
    public record HubInfo
    {
        public Guid Guid { get; init; }
        public int Visitors { get; init; }
        public int Locked { get; init; }
        public string[] Commanders { get; init; }
    }
}
