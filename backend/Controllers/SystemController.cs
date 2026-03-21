using ArangoDBNetStandard;
using Microsoft.AspNetCore.Mvc;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly IArangoDBClient _dbClient;

        public SystemController(IArangoDBClient dbClient)
        {
            _dbClient = dbClient;
        }

        [HttpGet("health-check")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var dbInfo = await _dbClient.Database.GetCurrentDatabaseInfoAsync();

                return Ok(new
                {
                    Status = "Healthy",
                    Message = "Backend and ArangoDB is running!",
                    ActiveDatabase = dbInfo.Result.Name,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Dead",
                    Message = "Database corrupted! Checking ArangoDB.",
                    Error = ex.Message
                });
            }
        }
    }
}