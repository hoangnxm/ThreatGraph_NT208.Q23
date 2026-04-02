using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IocNodes.DTOs;
using IocNodes.Services;

namespace IocNodes.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route sẽ tự map thành: api/iocnodes
    public class IocNodesController : ControllerBase
    {
        private readonly IIocNodeService _service;

        public IocNodesController(IIocNodeService service)
        {
            _service = service;
        }

        // GET: api/iocnodes?offset=0&limit=50
        [HttpGet]
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
        public async Task<IActionResult> Create([FromBody] CreateIocNodeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateAsync(request);
            
            // Trả về HTTP 201 Created kèm header Location trỏ tới URL của resource vừa tạo
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: api/iocnodes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateIocNodeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.UpdateAsync(id, request);
            if (result == null) return NotFound(new { Message = $"Không tìm thấy IOC với ID: {id} để update" });
            
            return Ok(result);
        }

        // DELETE: api/iocnodes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = $"Không tìm thấy IOC với ID: {id} để xóa" });
            
            // Xóa thành công thì trả về HTTP 204 No Content là chuẩn bài RESTful nhất
            return NoContent(); 
        }
    }
}