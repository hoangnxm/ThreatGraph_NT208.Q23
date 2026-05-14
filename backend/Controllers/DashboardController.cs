using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NT208_Project.Controllers
{
    // Class Dto này để hứng dữ liệu gộp từ ArangoDB, chống lỗi mất biểu đồ
    public class TypeDistDto 
    {
        public string? Type { get; set; }
        public int Count { get; set; }
    }

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
            
            // Đếm số IOC thêm hôm nay (bọc lót CreatedAt và createdAt)
            string todayQuery = @"
                RETURN LENGTH(
                    FOR doc IN IocNodes 
                    FILTER LEFT(doc.CreatedAt, 10) == LEFT(DATE_ISO8601(DATE_NOW()), 10) 
                        OR LEFT(doc.createdAt, 10) == LEFT(DATE_ISO8601(DATE_NOW()), 10) 
                    RETURN doc
                )";
            var iocsToday = await _db.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = todayQuery });

            // Lấy Top 10 IOC chung (không giới hạn loại IP nữa)
            string topIocsQuery = @"
                FOR doc IN IocNodes 
                SORT doc.RiskScore DESC, doc.riskScore DESC 
                LIMIT 10 
                RETURN doc";
            var topIocs = await _db.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = topIocsQuery });
            
            // Đếm số lượng từng loại mã độc để vẽ Pie Chart
            string distributionQuery = @"
                FOR doc IN IocNodes 
                COLLECT iocType = UPPER(doc.type ? doc.type : doc.Type) WITH COUNT INTO count 
                RETURN { Type: iocType, Count: count }";
            var distribution = await _db.Cursor.PostCursorAsync<TypeDistDto>(new PostCursorBody { Query = distributionQuery });

            return Ok(new 
            { 
                TotalUsers = usersCount.Result.FirstOrDefault(), 
                TotalLogs = logsCount.Result.FirstOrDefault(),
                TotalIocs = totalIocs.Result.FirstOrDefault(),
                IocsToday = iocsToday.Result.FirstOrDefault(),
                TotalEdges = totalEdges.Result.FirstOrDefault(),
                TopIocs = topIocs.Result,
                TypeDistribution = distribution.Result 
            });
        }
    }
}