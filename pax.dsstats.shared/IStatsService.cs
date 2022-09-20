using pax.dsstats.shared;

namespace pax.dsstats.shared;

public interface IStatsService
{
    Task<StatsResponse> GetStatsResponse(StatsRequest request);
    void ResetCache();
}