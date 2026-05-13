using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IocNodes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace IocNodes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DataFeedsController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Dùng IServiceScopeFactory thay vì IDataFeedService trực tiếp để tránh lỗi Memory Leak
        // khi chạy tiến trình ngầm tách biệt khỏi HTTP Request
        public DataFeedsController(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        [HttpPost("sync/alienvault")]
        public IActionResult SyncAlienVault()
        {
            // Bắn một Task chạy độc lập vào RAM của VPS
            Task.Run(async () =>
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    // Lấy service mới bên trong scope ngầm này
                    var dataFeedService = scope.ServiceProvider.GetRequiredService<IDataFeedService>();
                    await dataFeedService.SyncAlienVaultDataAsync();
                }
            });

            // Lập tức phản hồi 202 Accepted (Đã chấp nhận yêu cầu) cho Frontend không phải đợi
            return Accepted(new { Message = "Đã nhận lệnh cào dữ liệu. Hệ thống đang tiến hành đồng bộ hàng chục ngàn IOCs ở chế độ chạy ngầm (Background). Quá trình này có thể mất nhiều phút, vui lòng kiểm tra Logs trên Dashboard để theo dõi tiến độ." });
        }
    }
}