using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IReplayRepository replayRepository;
        private readonly IStatsRepository statsRepository;
        private readonly BuildService buildService;

        public StatsController(IReplayRepository replayRepository, IStatsRepository statsRepository, BuildService buildService)
        {
            this.replayRepository = replayRepository;
            this.statsRepository = statsRepository;
            this.buildService = buildService;
        }

        [HttpPost]
        [Route("GetReplay")]
        public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
        {
            var replayDto = await replayRepository.GetReplay(replayHash);
            if (replayDto == null)
            {
                return null;
            }
            return replayDto;
        }

        [HttpPost]
        [Route("GetReplaysCount")]
        public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
        {
            return await replayRepository.GetReplaysCount(request, token);
        }

        [HttpPost]
        [Route("GetReplays")]
        public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
        {
            return await replayRepository.GetReplays(request, token);
        }

        [HttpPost]
        [Route("GetStats")]
        public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
        {
            return (StatsResponse)await statsRepository.GetStats(request, token);
        }

        [HttpPost]
        [Route("GetBuild")]
        public async Task<BuildResponse> GetBuild(BuildRequest request)
        {
            return await buildService.GetBuild(request);
        }

    }

}
