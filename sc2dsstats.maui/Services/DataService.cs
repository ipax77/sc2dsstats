using Microsoft.AspNetCore.Components;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace sc2dsstats.maui.Services;

public class DataService : IDataService
{
    private readonly IReplayRepository replayRepository;
    private readonly IStatsRepository statsRepository;
    private readonly BuildService buildService;
    private readonly IStatsService statsService;

    public DataService(IReplayRepository replayRepository, IStatsRepository statsRepository, BuildService buildService, IStatsService statsService)
    {
        this.replayRepository = replayRepository;
        this.statsRepository = statsRepository;
        this.buildService = buildService;
        this.statsService = statsService;
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        var replayDto = await replayRepository.GetReplay(replayHash);
        if (replayDto == null)
        {
            return null;
        }
        return replayDto;
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        return await replayRepository.GetReplaysCount(request, token);
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        return await replayRepository.GetReplays(request, token);
    }

    public async Task<ICollection<string>> GetReplayPaths()
    {
        return await replayRepository.GetReplayPaths();
    }

    public async Task<List<string>> GetTournaments()
    {
        return await replayRepository.GetTournaments();
    }

    public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
    {
        return await statsService.GetStatsResponse(request);
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request)
    {
        return await buildService.GetBuild(request);
    }

    public async Task<int> GetRatingsCount(CancellationToken token = default)
    {
        return await statsService.GetRatingsCount(token);
    }

    public async Task<List<PlayerRatingDto>> GetRatings(int skip, int take, List<TableOrder> orders, CancellationToken token = default)
    {
        return await statsService.GetRatings(skip, take, orders, token);
    }

    public async Task<string?> GetPlayerRatings(int toonId)
    {
        return await statsService.GetPlayerRatings(toonId);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        return await statsService.GetRatingsDeviation();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        return await statsService.GetRatingsDeviationStd();
    }

}
