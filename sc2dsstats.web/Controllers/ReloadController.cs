using Microsoft.AspNetCore.Mvc;
using sc2dsstats.lib.Data;
using System.Threading.Tasks;

namespace sc2dsstats.web.Controllers
{
    [ServiceFilter(typeof(ReloadFilterAttribute))]
    [ApiController]
    public class ReloadController : ControllerBase
    {
        private LoadData _data { get; set; }

        public ReloadController(LoadData data)
        {
            _data = data;
        }

        [HttpGet("secure/reload")]
        public async Task<IActionResult> ReloadData()
        {
            _data.Init();
            return Ok();
        }
    }
}
