namespace sc2dsstats.lib.Models
{
    public class DatasetInfo
    {
        public string Dataset { get; set; }
        public int Count { get; set; } = 0;
        public float Teamgames { get; set; } = 0;
        public float Winrate { get; set; } = 0;
    }
}
