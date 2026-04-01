using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NT208_Project.Controllers;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly IArangoDBClient dbClient;

        public SearchController(IArangoDBClient dbclient)
        {
            dbClient = dbclient;
        }

        // API sẽ tìm từ khoá. Link gọi : Get api/search/{keyword}
        [HttpGet("{keyword}")]
        public async Task<IActionResult> GlobalSearch(string keyword)
        {
            try
            {
                // Truy vấn
                string Querry = @"
                    FOR node IN IocNodes
                    FILTER node.Value == @keyword OR LIKE(node.Value, CONCAT('%', @keyword, '%'), true)
                    LIMIT 1
                    RETURN node
                ";

                // Chống AQL Injection
                var bindVars = new Dictionary<string, object>
                {
                    { "keyword", keyword }
                };

                // Gửi lệnh qua ArangoDB
                var response = await dbClient.Cursor.PostCursorAsync<IocNode>(
                    new PostCursorBody 
                    {
                        Query = Querry,
                        BindVars = bindVars
                    }
                );

                var result = response.Result.FirstOrDefault();

                // Nếu khôngg có
                if (result == null)
                {
                    return NotFound(new { message = "Không tìm thấy dấu vết mã độc!!" });
                }

                return Ok(result);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tra cứu", error = ex.Message });
            }
        }
    }
}
