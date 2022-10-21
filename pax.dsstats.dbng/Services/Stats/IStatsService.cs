using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services
{
    public interface IStatsService
    {
        Task<StatsResponse> GetCustomTimeline(StatsRequest request);
        Task<StatsResponse> GetCustomWinrate(StatsRequest request);
        Task<string?> GetPlayerRatings(int toonId);
        Task<List<PlayerRatingDto>> GetRatings(int skip, int take, Order order, CancellationToken token = default);
        Task<int> GetRatingsCount(CancellationToken token = default);
        Task<List<MmrDevDto>> GetRatingsDeviation();
        Task<StatsResponse> GetStatsResponse(StatsRequest request);
        void ResetCache();
    }
}