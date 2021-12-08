namespace sc2dsstats.db.Services;

public interface IInsertService
{
    event EventHandler<InsertEventArgs> ReplaysInserted;

    bool AddReplay(Dsreplay replay);
    void AddReplays(List<Dsreplay> replays);
    void Cancel();
    void Dispose();
    void Reset();
    void WriteFinished();
    void WriteStart();
}

public class InsertEventArgs : EventArgs
{
    public int insertCount { get; set; }
    public bool Done { get; set; } = false;
}

