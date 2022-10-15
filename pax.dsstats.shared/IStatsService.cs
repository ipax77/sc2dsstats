using pax.dsstats.shared;

namespace pax.dsstats.shared;

public interface IStatsService
{
    Task<StatsResponse> GetStatsResponse(StatsRequest request);
    void ResetCache();

    // Ratings
    Task<int> GetRatingsCount(CancellationToken token = default);
    Task<List<PlayerRatingDto>> GetRatings(int skip, int take, Order order, CancellationToken token = default);
    Task<string?> GetPlayerRatings(int toonId);
}