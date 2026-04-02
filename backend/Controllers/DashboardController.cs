using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IArangoDBClient _db;
        public DashboardController(IArangoDBClient db) { _db = db; }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var users = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(Users)" });
            var logs = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(AuditLogs)" });
            
            return Ok(new { TotalUsers = users.Result.FirstOrDefault(), TotalLogs = logs.Result.FirstOrDefault() });
        }
    }
}