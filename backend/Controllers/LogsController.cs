using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được xem nhật ký
    public class LogsController : ControllerBase
    {
        private readonly IArangoDBClient _db;
        public LogsController(IArangoDBClient db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            // Lấy logs và sắp xếp mới nhất lên đầu (DESC)
            var cursor = await _db.Cursor.PostCursorAsync<dynamic>(new PostCursorBody { 
                Query = "FOR l IN AuditLogs SORT l.Timestamp DESC RETURN l" 
            });
            return Ok(cursor.Result);
        }
    }
}