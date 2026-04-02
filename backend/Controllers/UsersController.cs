using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models; 
namespace NT208_Project.Controllers
{
    public class UserModel
    {
        public string _key { get; set; }
        public string _id { get; set; }
        public string _rev { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string role { get; set; }
        public bool isLocked { get; set; }
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