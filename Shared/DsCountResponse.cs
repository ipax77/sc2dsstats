namespace sc2dsstats._2022.Shared
{
    public class DsCountResponse
    {
        public int TotalCount { get; set; }
        public int CmdrCount { get; set; }
        public int StdCount { get; set; }
        public int FilteredCount { get; set; }
        public double Leaver { get; set; }
        public double Quits { get; set; }
    }
}
