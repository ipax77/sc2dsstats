namespace sc2dsstats.decode
{
    public class DecodeStateEvent : EventArgs
    {
        public int Threads = 0;
        public int Done = 0;
        public int Failed = 0;
        public bool Running = false;
        public DateTime StartTime = DateTime.UtcNow;
    }
}
