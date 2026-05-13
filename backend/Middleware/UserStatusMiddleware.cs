using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using System.Security.Claims;

namespace NT208_Project.Middlewares
{
    public class UserStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public UserStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IArangoDBClient db)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var username = context.User.FindFirst(ClaimTypes.Name)?.Value;

                var jwtSessionToken = context.User.FindFirst("SessionToken")?.Value;

                if (!string.IsNullOrEmpty(username))
                {
                    var cursor = await db.Cursor.PostCursorAsync<dynamic>(new PostCursorBody
                    {
                        Query = "FOR u IN Users FILTER u.username == @usr RETURN u",
                        BindVars = new Dictionary<string, object> { { "usr", username } }
                    });

                    var user = cursor.Result.FirstOrDefault();

                    if (user == null || user.isLocked == true)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { message = "Tài khoản của bạn đã bị xóa hoặc bị khóa bởi Admin!" });
                        return;
                    }

                    string dbSessionToken = user.sessionToken != null ? (string)user.sessionToken : "";

                    if (!string.IsNullOrEmpty(dbSessionToken) && dbSessionToken != jwtSessionToken)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { message = "Tài khoản của bạn vừa được đăng nhập trên một thiết bị khác!" });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}