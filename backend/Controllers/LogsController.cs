using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using System;

namespace NT208_Project.Controllers
{    public class AuditLogModel
    {
        public string? _key { get; set; }
        public string? _id { get; set; }
        public string? _rev { get; set; }
        
        public DateTime Timestamp { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string ClientIp { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly IArangoDBClient _db;
        public LogsController(IArangoDBClient db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var cursor = await _db.Cursor.PostCursorAsync<AuditLogModel>(new PostCursorBody { 
                Query = "FOR l IN AuditLogs SORT l.Timestamp DESC RETURN l" 
            });
            return Ok(cursor.Result);
        }
    }
}