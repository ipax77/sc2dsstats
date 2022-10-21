namespace pax.dsstats.shared;
public interface IDataService
{
    Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default);
    Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default);
    Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default);
    Task<ICollection<string>> GetReplayPaths();
    Task<List<string>> GetTournaments();
    Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default);
    Task<BuildResponse> GetBuild(BuildRequest request);

    // ratings
    Task<int> GetRatingsCount(CancellationToken token = default);
    Task<List<PlayerRatingDto>> GetRatings(int skip, int take, Order order, CancellationToken token = default);
    Task<string?> GetPlayerRatings(int toonId);
    Task<List<MmrDevDto>> GetRatingsDeviation();
}
