namespace sc2dsstats.db.Services;

public interface IInsertService
{
    event EventHandler<EventArgs> ReplaysInserted;

    bool AddReplay(Dsreplay replay);
    void AddReplays(List<Dsreplay> replays);
    void Cancel();
    void Dispose();
    void Reset();
    void WriteFinished();
    void WriteStart();
}


