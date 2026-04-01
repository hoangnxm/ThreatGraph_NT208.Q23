using ArangoDBNetStandard;
using Microsoft.AspNetCore.Mvc;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly IArangoDBClient _db;
        public SystemController(IArangoDBClient db) { _db = db; }

        [HttpGet("health-check")]
        public async Task<IActionResult> HealthCheck()
        {
            try {
                var dbInfo = await _db.Database.GetCurrentDatabaseInfoAsync();
                return Ok(new { Status = "Healthy", ActiveDatabase = dbInfo.Result.Name, Timestamp = DateTime.UtcNow });
            } catch (Exception ex) {
                return StatusCode(500, new { Status = "Dead", Error = ex.Message });
            }
        }
    }
}