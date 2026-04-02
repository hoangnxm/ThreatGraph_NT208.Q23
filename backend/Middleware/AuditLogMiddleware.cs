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
            await _next(context);

            var method = context.Request.Method;
            if (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH")
            {
                var username = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var path = context.Request.Path;
                
                // Bắt IP thật
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
                
                if (ip == "::1") ip = "127.0.0.1";

                var logEntry = new {
                    Timestamp = DateTime.UtcNow,
                    Action = method,
                    Resource = path,
                    Username = username,    
                    ClientIp = ip,      
                    StatusCode = context.Response.StatusCode
                };
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