using System.Collections.Generic;

namespace sc2dsstats._2022.Shared
{
    public class TimelineResponse : DsResponse
    {
        public string Versus { get; set; }
        public List<double> SmaData { get; set; }
    }

    public class TimelineResponseItem : DsResponseItem
    {
    }

}
