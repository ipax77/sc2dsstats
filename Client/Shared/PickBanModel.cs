namespace sc2dsstats._2022.Client.Shared
{
    public class PickBanModel
    {
        public Guid Guid { get; set; }
        public string[] Picks { get; set; } = new string[6] { null, null, null, null, null, null };
        public int Pos { get; set; }
        public int Locks { get; set; }
    }

    public class PickBanLockModel
    {
        public Guid Guid { get; set; }
        public int i { get; set; }
        public string cmdr { get; set; }
    }

    public class PickBanInfo
    {
        public Guid Guid { get; set; }
        public List<string> Clients { get; set; }
    }
}
