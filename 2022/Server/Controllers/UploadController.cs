using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Server.Attributes;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using System.Text.RegularExpressions;

namespace sc2dsstats._2022.Server.Controllers
{
    [ServiceFilter(typeof(AuthenticationFilterAttribute))]
    [ApiController]
    public class UploadController : ControllerBase
    {
        Regex r = new Regex("[^A-Za-z0-9]$");

        private readonly sc2dsstatsContext context;
        private readonly InsertService insertService;
        private readonly CacheService cacheService;
        private readonly ILogger<UploadController> logger;

        public UploadController(sc2dsstatsContext context, InsertService insertService, CacheService cacheService, ILogger<UploadController> logger)
        {
            this.context = context;
            this.insertService = insertService;
            this.cacheService = cacheService;
            this.logger = logger;
        }

        [HttpPost]
        [Route("secure/data/uploadrequest")]
        public async Task<ActionResult<DateTime>> UploadRequest([FromBody] DsUploadRequest uploadRequest)
        {
            var player = await PlayerService.GetPlayerName(context, uploadRequest);
            return player.LatestReplay;
        }

        [HttpPost]
        [RequestSizeLimit(102400000)]
        [Route("secure/data/replayupload/{guid}")]
        public async Task<ActionResult<DsUploadResponse>> ReplayUpload([FromBody] string gzipBase646Replays, Guid guid)
        {
            var player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.AppId == guid);
            if (player == null)
            {
                return NotFound();
            }

            List<DsReplayDto> replays;
            try
            {
                var json = DSData.Unzip(gzipBase646Replays);
                replays = System.Text.Json.JsonSerializer.Deserialize<List<DsReplayDto>>(json);
            }
            catch (Exception e)
            {
                logger.LogError($"Failed handling upload body from {guid}: {e.Message}");
                return BadRequest();
            }

            logger.LogInformation($"Got replays from {guid}: {replays.Count}");

            if (replays.Any())
            {
                player.LatestReplay = replays.OrderByDescending(o => o.GAMETIME).First().GAMETIME;
                await context.SaveChangesAsync();
                insertService.InsertReplays(replays, player);
                cacheService.Updatesavailable = true;
            }
            return new DsUploadResponse()
            {
                DbId = player.DbId
            };
        }

        [HttpPost]
        [Route("secure/data/autoinfo")]
        public async Task<ActionResult<DateTime>> GetInfo([FromBody] DsUploadInfo info)
        {
            if (info.Name.Length != 64)
                return BadRequest();
            if (r.IsMatch(info.Name))
                return BadRequest();

            var player = await PlayerService.GetPlayerName(context, info);
            return player.LatestReplay;
        }

        [HttpPost]
        [Route("secure/data/dbupload/{id}")]
        public async Task<ActionResult<DateTime>> GetReplays(string id)
        {
            if (id.Length != 64)
                return BadRequest();
            if (r.IsMatch(id))
                return BadRequest();

            var info = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Hash == id);
            if (info == null)
            {
                return NotFound();
            }

            if (Request.Form.Files.Count != 1)
                return BadRequest("We need one file.");
            var file = Request.Form.Files.First();
            if (file.Length > 104857600 || file.Length == 0)
                return BadRequest("File Size.");

            List<DsReplayDto> replays;

            using (var stream = new MemoryStream((int)file.Length))
            {
                await file.CopyToAsync(stream);
                replays = await insertService.Decompress(stream);
            }

            if (replays == null || !replays.Any())
            {
                return NotFound();
            }

            info.LatestReplay = replays.First().GAMETIME;
            info.TotlaReplays += replays.Count();
            info.LatestUpload = DateTime.UtcNow;
            await context.SaveChangesAsync();

            insertService.InsertReplays(replays, id);

            return Ok();
        }

        [HttpPost]
        [RequestSizeLimit(102400000)]
        [Route("secure/data/upload/{id}")]
        public async Task<IActionResult> GetNewReplays([FromBody] string gzipBase646Replays, string id)
        {
            var info = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Hash == id);
            if (info == null)
            {
                return NotFound();
            }

            List<DsReplayDto> replays;
            try
            {
                var json = DSData.Unzip(gzipBase646Replays);
                replays = System.Text.Json.JsonSerializer.Deserialize<List<DsReplayDto>>(json);
            }
            catch (Exception e)
            {
                logger.LogError($"Failed handling upload body from {id}: {e.Message}");
                return BadRequest();
            }

            logger.LogInformation($"Got replays from {id}: {replays.Count}");

            if (replays.Any())
            {
                info.LatestReplay = replays.OrderByDescending(o => o.GAMETIME).First().GAMETIME;
                info.TotlaReplays += replays.Count;
                info.LatestUpload = DateTime.UtcNow;
                await context.SaveChangesAsync();
                insertService.InsertReplays(replays, info);
                cacheService.Updatesavailable = true;
            }

            return Ok();
        }

        [HttpGet]
        [Route("secure/data/unitnames/{namedate}")]
        public async Task<ActionResult<List<NameResponse>>> GetUnitNames(DateTime namedate)
        {
            var info = await context.DsInfo.FirstAsync();
            if (info.UnitNamesUpdate > namedate)
            {
                return await context.UnitNames.AsNoTracking().Select(s => new NameResponse()
                {
                    sId = s.sId,
                    Name = s.Name
                }).ToListAsync();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("secure/data/upgradenames/{upgradedate}")]
        public async Task<ActionResult<List<NameResponse>>> GetUpgradeNames(DateTime upgradedate)
        {
            var info = await context.DsInfo.FirstAsync();
            if (info.UpgradeNamesUpdate > upgradedate)
            {
                return await context.UpgradeNames.AsNoTracking().Select(s => new NameResponse()
                {
                    sId = s.sId,
                    Name = s.Name
                }).ToListAsync();
            }
            else
            {
                return NotFound();
            }
        }

    }
}
