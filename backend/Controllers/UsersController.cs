using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models; 
namespace NT208_Project.Controllers
{
    // Định nghĩa Model chuẩn xác cho User
    public class UserModel
    {
        // Thêm dấu ? để cho phép giá trị null khi thêm mới
        public string? _key { get; set; }
        public string? _id { get; set; }
        public string? _rev { get; set; }
        
        public string username { get; set; }
        public string password { get; set; }
        public string? role { get; set; }
        public bool? isLocked { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IArangoDBClient _db;

        public UsersController(IArangoDBClient db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cursor = await _db.Cursor.PostCursorAsync<UserModel>(new PostCursorBody 
            { 
                Query = "FOR u IN Users RETURN u" 
            });
            
            return Ok(cursor.Result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserModel newUser)
        {
            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newUser.password);
                var documentToInsert = new 
                {
                    username = newUser.username,
                    password = hashedPassword,
                    role = string.IsNullOrEmpty(newUser.role) ? "User" : newUser.role,
                    isLocked = false
                };

                await _db.Cursor.PostCursorAsync<object>(new PostCursorBody 
                {
                    Query = "INSERT @doc INTO Users RETURN NEW",
                    BindVars = new Dictionary<string, object> { { "doc", documentToInsert } }
                });

                return Ok(new { message = "Tạo tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo User: " + ex.Message });
            }
        }
        [HttpPut("{key}")]
        public async Task<IActionResult> Update(string key, [FromBody] UserModel updateData)
        {
            try
            {
                string updateQuery;
                var bindVars = new Dictionary<string, object>
                {
                    { "key", key },
                    { "username", updateData.username },
                    { "role", string.IsNullOrEmpty(updateData.role) ? "User" : updateData.role },
                    { "isLocked", updateData.isLocked ?? false }
                };

                if (!string.IsNullOrEmpty(updateData.password))
                {
                    updateQuery = "UPDATE @key WITH { username: @username, password: @password, role: @role, isLocked: @isLocked } IN Users";
                    bindVars.Add("password", BCrypt.Net.BCrypt.HashPassword(updateData.password));
                }
                else
                {
                    updateQuery = "UPDATE @key WITH { username: @username, role: @role, isLocked: @isLocked } IN Users";
                }

                await _db.Cursor.PostCursorAsync<object>(new PostCursorBody 
                {
                    Query = updateQuery,
                    BindVars = bindVars
                });

                return Ok(new { message = "Đã cập nhật tài khoản!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật User: " + ex.Message });
            }
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            await _db.Cursor.PostCursorAsync<object>(new PostCursorBody 
            {
                Query = "REMOVE @key IN Users",
                BindVars = new Dictionary<string, object> { { "key", key } }
            });
            return Ok(new { message = "Đã xóa tài khoản!" });
        }
    }
}