using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using sc2dsstats.lib.Models;
using sc2dsstats.rest.Attributes;
using sc2dsstats.rest.Models;
using sc2dsstats.rest.Repositories;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.rest.Controllers
{
    [ServiceFilter(typeof(AuthenticationFilterAttribute))]
    [ApiController]
    public class SecureDataController : ControllerBase
    {
        Regex r = new Regex("[^A-Za-z0-9]$");

        private readonly IDataRepository _dataRepository;
        private readonly ILogger _logger;

        public SecureDataController(IDataRepository dataRepository, ILogger<SecureDataController> logger)
        {
            _dataRepository = dataRepository;
            _logger = logger;
        }

        [HttpPost("secure/data/autoinfo")]
        public async Task<IActionResult> GetAutoInfo([FromBody]DSinfo info)
        {
            _logger.LogInformation("Getting info from {id}", info.Name);
            if (info.Name.Length != 64) return BadRequest("Wrong id.");
            if (r.IsMatch(info.Name)) return BadRequest("Wrong id.");
            return Ok(_dataRepository.AutoInfo(info));
        }

        [HttpPost("secure/data/autoupload/{id}")]
        public async Task<IActionResult> AutoUpload(string id)
        {
            _logger.LogInformation("Getting data from {id}", id);
            if (id.Length != 64) return BadRequest("Wrong id.");
            if (r.IsMatch(id)) return BadRequest("Wrong id.");

            if (Request.Form.Files.Count != 1) return BadRequest("We need one file.");
            long size = Request.Form.Files.Sum(f => f.Length);
            if (size > 104857600) return BadRequest("File Size.");

            // full path to file in temp location
            var filePath = Path.GetTempFileName();

            foreach (var formFile in Request.Form.Files)
            {

                if (formFile.Length > 0)
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }
                bool fileok = await _dataRepository.GetAutoFile(id, filePath);
                if (fileok == false)
                {
                    return BadRequest("Wrong file.");
                }
            }

            // process uploaded files
            // Don't rely on or trust the FileName property without validation.

            return Ok();
        }

        [HttpPost("secure/data/dbupload/{id}")]
        public async Task<IActionResult> DBUpload(string id)
        {
            _logger.LogInformation("Getting data from {id}", id);
            if (id.Length != 64) return BadRequest("Wrong id.");
            if (r.IsMatch(id)) return BadRequest("Wrong id.");

            if (Request.Form.Files.Count != 1) return BadRequest("We need one file.");
            long size = Request.Form.Files.Sum(f => f.Length);
            if (size > 10485760) return BadRequest("File Size.");

            // full path to file in temp location
            var filePath = Path.GetTempFileName();

            foreach (var formFile in Request.Form.Files)
            {

                if (formFile.Length > 0)
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }
                bool fileok = await _dataRepository.GetDBFile(id, filePath);
                if (fileok == false)
                {
                    return BadRequest("Wrong file.");
                }
            }

            // process uploaded files
            // Don't rely on or trust the FileName property without validation.

            return Ok();
        }
    }
}