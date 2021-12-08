namespace sc2dsstats._2022.Shared
{
    public class TimelineRequest : DsRequest
    {
        public TimelineRequest() : base()
        { }

        public TimelineRequest(string mode, string timespan, bool player, string interest = "ALL", string versus = "ALL")
            : base(mode, timespan, player, interest, versus)
        { }

        public int Step { get; set; } = 500;
        public int smaK { get; set; } = 6;
    }
}
