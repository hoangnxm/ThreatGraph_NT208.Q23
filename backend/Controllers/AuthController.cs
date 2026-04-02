using Microsoft.AspNetCore.Mvc;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IArangoDBClient _db;
        private readonly IConfiguration _config;

        public AuthController(IArangoDBClient db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            try 
            {
                // Kiểm tra xem User có trong ArangoDB không
                var cursor = await _db.Cursor.PostCursorAsync<dynamic>(new PostCursorBody
                {
                    Query = "FOR u IN Users FILTER u.username == @usr RETURN u",
                    BindVars = new Dictionary<string, object> { { "usr", req.Username } }
                });

                var user = cursor.Result.FirstOrDefault();

                if (user == null)
                {
                    return StatusCode(401, new { message = "Sai tài khoản hoặc mật khẩu!" });
                }

                // Check mật khẩu (Đồ án đang để thô chưa hash thì dùng ==)
                // Nếu user null hoặc sai pass -> Đuổi!
                // Lấy password từ DB ra
                string dbPassword = (string)user.password ?? "";
            // DÙNG BCRYPT ĐỂ GIẢI MÃ VÀ SO SÁNH
            if (!BCrypt.Net.BCrypt.Verify(req.Password, dbPassword))
            {
                 return StatusCode(401, new { message = "Sai tài khoản hoặc mật khẩu!" });
                 }

                if (user.isLocked == true) return StatusCode(403, new { message = "Tài khoản đang bị khóa !" });

                // Lấy Role từ DB (nếu không có thì mặc định cho làm Admin để test)
                string role = user.role ?? "Admin";

                // Nhả Token
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    claims: new[] { new Claim(ClaimTypes.Name, (string)user.username), new Claim(ClaimTypes.Role, role) },
                    expires: DateTime.Now.AddHours(2),
                    signingCredentials: creds
                );

                return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), role = role });
            }
            catch (Exception ex)
            {
                // Báo lỗi
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }

    public class LoginRequest { public string Username { get; set; } public string Password { get; set; } }
}