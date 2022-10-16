using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public class DataService : IDataService
{
    private readonly IReplayRepository replayRepository;
    private readonly IStatsRepository statsRepository;

    public DataService(IReplayRepository replayRepository, IStatsRepository statsRepository)
    {
        this.replayRepository = replayRepository;
        this.statsRepository = statsRepository;
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
}
