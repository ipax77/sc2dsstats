using Microsoft.AspNetCore.Mvc;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using sc2dsstats.db.Services;

namespace sc2dsstats._2022.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReplaysController : ControllerBase
    {
        private readonly sc2dsstatsContext context;
        private readonly ILogger<ReplaysController> logger;

        public ReplaysController(sc2dsstatsContext context, ILogger<ReplaysController> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<List<DsReplayResponse>>> ReplayRequest(DsReplayRequest request, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                return await ReplayService.GetReplays(context, request, cancellationToken);
            }
            else
            {
                return Ok();
            }
        }

        [HttpPost("count")]
        public async Task<ActionResult<int>> CountRequest(DsReplayRequest request)
        {
            return await ReplayService.GetCount(context, request);
        }

        [HttpGet("{hash}")]
        public async Task<ActionResult<DsGameResponse>> GetReplay(string hash)
        {
            var replay = await ReplayService.GetReplay(context, hash);
            if (replay == null)
                return NotFound();
            return replay;
        }
    }
}
