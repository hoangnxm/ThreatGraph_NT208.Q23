using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Cả User và Admin đều vào được
    public class DashboardController : ControllerBase
    {
        private readonly IArangoDBClient _db;
        public DashboardController(IArangoDBClient db) { _db = db; }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            // Dữ liệu dành cho Admin
            var usersCount = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(Users)" });
            var logsCount = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(AuditLogs)" });

            // Dữ liệu IOC chung
            var totalIocs = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(IocNodes)" });
            var totalEdges = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = "RETURN LENGTH(IocRelationships)" });
            
            // Lọc IOC thêm vào hôm nay (Giả sử định dạng CreatedAt là ISO8601)
            string todayQuery = "RETURN LENGTH(FOR doc IN IocNodes FILTER LEFT(doc.CreatedAt, 10) == LEFT(DATE_ISO8601(DATE_NOW()), 10) RETURN doc)";
            var iocsToday = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = todayQuery });

            // 3. Top 10 IP nguy hiểm nhất
            string topIpsQuery = @"
            FOR doc IN IocNodes 
            FILTER doc.type == 'IP' OR doc.Type == 'IP' 
            SORT doc.riskScore DESC, doc.RiskScore DESC 
            LIMIT 10 
            RETURN doc";
            var topIps = await _db.Cursor.PostCursorAsync<object>(new PostCursorBody { Query = topIpsQuery });
            return Ok(new 
            { 
                TotalUsers = usersCount.Result.FirstOrDefault(), 
                TotalLogs = logsCount.Result.FirstOrDefault(),
                TotalIocs = totalIocs.Result.FirstOrDefault(),
                IocsToday = iocsToday.Result.FirstOrDefault(),
                TotalEdges = totalEdges.Result.FirstOrDefault(),
                TopIps = topIps.Result
            });
        }
    }
}