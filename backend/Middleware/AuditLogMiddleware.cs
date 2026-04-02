using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using System.Security.Claims;

namespace NT208_Project.Middlewares
{
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IArangoDBClient db)
        {
            await _next(context); // Chạy request trước

            // Chỉ ghi log nếu là các hành động thay đổi dữ liệu (POST, PUT, DELETE, PATCH)
            var method = context.Request.Method;
            if (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH")
            {
                var username = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Guest/Unknown";
                var path = context.Request.Path;
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

                var logEntry = new {
                    Timestamp = DateTime.UtcNow,
                    Action = method,
                    Resource = path,
                    User = username,
                    IPAddress = ip,
                    StatusCode = context.Response.StatusCode
                };

                // Lưu vào Collection AuditLogs
                try {
                    await db.Cursor.PostCursorAsync<object>(new PostCursorBody {
                        Query = "INSERT @log INTO AuditLogs",
                        BindVars = new Dictionary<string, object> { { "log", logEntry } }
                    });
                } catch { /* Bỏ qua nếu lỗi ghi log để không sập app */ }
            }
        }
    }
}