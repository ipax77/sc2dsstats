using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc2dsstats.maui.Services;

public class DataService : IDataService
{
    private readonly IReplayRepository replayRepository;
    private readonly IStatsRepository statsRepository;
    private readonly BuildService buildService;

    public DataService(IReplayRepository replayRepository, IStatsRepository statsRepository, BuildService buildService)
    {
        this.replayRepository = replayRepository;
        this.statsRepository = statsRepository;
        this.buildService = buildService;
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
        return (StatsResponse)await statsRepository.GetStats(request, token);
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request)
    {
        return await buildService.GetBuild(request);
    }
}
