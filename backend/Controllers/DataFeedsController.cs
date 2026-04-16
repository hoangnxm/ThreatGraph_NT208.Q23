using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IocNodes.Services;

namespace IocNodes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataFeedsController : ControllerBase
    {
        private readonly IDataFeedService _dataFeedService;

        public DataFeedsController(IDataFeedService dataFeedService)
        {
            _dataFeedService = dataFeedService;
        }

        [HttpPost("sync/alienvault")]
        public async Task<IActionResult> SyncAlienVault()
        {
            var count = await _dataFeedService.SyncAlienVaultDataAsync();
            return Ok(new { Message = $"Đồng bộ thành công {count} IOCs từ AlienVault OTX." });
        }
    }
}