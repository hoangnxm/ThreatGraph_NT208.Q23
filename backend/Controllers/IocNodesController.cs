using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IocNodes.DTOs;
using IocNodes.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace IocNodes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IocNodesController : ControllerBase
    {
        private readonly IIocNodeService _service;

        public IocNodesController(IIocNodeService service)
        {
            _service = service;
        }

        // GET: api/iocnodes?offset=0&limit=50
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] int offset = 0, [FromQuery] int limit = 50)
        {
            var result = await _service.GetAllAsync(offset, limit);
            return Ok(result);
        }

        // GET: api/iocnodes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { Message = $"Không tìm thấy IOC với ID: {id}" });
            return Ok(result);
        }

        // POST: api/iocnodes
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateIocNodeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Gọi xuống Service để tìm xem Value này đã có trong Database chưa
                var existingNode = await _service.GetByValueAsync(request.Value);

                if (existingNode != null)
                {
                    // Nếu đã tồn tại, trả về lỗi 409 Conflict cùng với thông tin của Node cũ
                    return StatusCode(409, new
                    {
                        message = $"Node '{request.Value}' đã tồn tại trong hệ thống!",
                        source = existingNode.OriginRef,
                        existingKey = existingNode.Id
                    });
                }

                var currentUser = User.Identity?.Name ?? "Unknown";
                request.OriginRef = currentUser;

                var result = await _service.CreateAsync(request);

                // Trả về HTTP 201 Created kèm header Location trỏ tới URL của resource vừa tạo
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo IOC", error = ex.Message });
            }
        }

        // POST: api/iocnodes/relationship
        [HttpPost("relationship")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRelationship([FromBody] CreateRelationshipRequest request)
        {
            // Kiểm tra xem Frontend có gửi thiếu trường nào trong DTO không
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var success = await _service.CreateRelationshipAsync(request);

                if (success)
                {
                    return Ok(new { message = "Tạo liên kết thành công!" });
                }

                return BadRequest(new { message = "Không thể tạo liên kết. Vui lòng kiểm tra lại Key của 2 Node." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // PUT: api/iocnodes/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateIocNodeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var currentUser = User.Identity?.Name;
                var isAdmin = User.IsInRole("Admin");

                // Lấy IOC để kiểm tra quyền
                var existingIoc = await _service.GetByIdAsync(id);
                if (existingIoc == null)
                    return NotFound(new { Message = $"Không tìm thấy IOC với ID: {id} để update" });

                // Phân quyền sửa
                if (!isAdmin && existingIoc.OriginRef != currentUser)
                {
                    return StatusCode(403, new { message = "Cảnh báo: Bạn không có quyền sửa dữ liệu do người khác tạo!" });
                }

                // Tiến hành update (giữ nguyên logic gọi service của ông)
                var result = await _service.UpdateAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi cập nhật", error = ex.Message });
            }
        }

        // DELETE: api/iocnodes/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            try
            {

                // Lấy thông tin user đang request
                var currentUser = User.Identity?.Name;
                var isAdmin = User.IsInRole("Admin");

                // Lấy dữ liệu
                var existingIoc = await _service.GetByIdAsync(id);
                if (existingIoc == null)
                {
                    return NotFound(new { message = "Không tìm thấy IOC này" });
                }

                // Phân quyền
                if (!isAdmin && existingIoc.OriginRef != currentUser)
                {
                    return StatusCode(403, new { message = "Cảnh báo: Bạn không có quyền xóa dữ liệu của người khác!" });
                }

                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa IOC thành công!" });
            }catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        
        // GET: api/iocnodes/paged?offset=0&limit=50&type=IP&keyword=103
        [HttpGet("paged")]
        [Authorize]
        public async Task<IActionResult> GetAllPaged([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? type = null, [FromQuery] string? keyword = null)
        {
            var result = await _service.GetAllPagedAsync(offset, limit, type, keyword);
            return Ok(result);
        }
    }
}