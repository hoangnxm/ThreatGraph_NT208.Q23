using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;

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
            var usersCount = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(Users)" });
            var logsCount = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(AuditLogs)" });
            var totalIocs = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(IocNodes)" });
            var totalEdges = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(IocRelationships)" });
            
            string todayQuery = "RETURN LENGTH(FOR doc IN IocNodes FILTER LEFT(doc.CreatedAt, 10) == LEFT(DATE_ISO8601(DATE_NOW()), 10) RETURN doc)";
            var iocsToday = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = todayQuery });

            // Truy vấn để lấy top 10 IOC có RiskScore cao nhất
            string topIocsQuery = @"
                FOR doc IN IocNodes 
                SORT doc.RiskScore DESC, doc.riskScore DESC 
                LIMIT 10 
                RETURN doc";
            var topIocs = await _db.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = topIocsQuery });
            
            return Ok(new 
            { 
                TotalUsers = usersCount.Result.FirstOrDefault(), 
                TotalLogs = logsCount.Result.FirstOrDefault(),
                TotalIocs = totalIocs.Result.FirstOrDefault(),
                IocsToday = iocsToday.Result.FirstOrDefault(),
                TotalEdges = totalEdges.Result.FirstOrDefault(),
                TopIocs = topIocs.Result // Đổi tên trả về thành TopIocs
            });
        }
    }
}