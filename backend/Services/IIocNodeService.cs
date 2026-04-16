using System.Collections.Generic;
using System.Threading.Tasks;
using IocNodes.DTOs;

namespace IocNodes.Services
{
    public interface IIocNodeService
    {
        Task<IocNodeResponse?> GetByIdAsync(string id);
        Task<IEnumerable<IocNodeResponse>> GetAllAsync(int offset, int limit);
        Task<IocNodeResponse> CreateAsync(CreateIocNodeRequest request);
        Task<IocNodeResponse?> UpdateAsync(string id, UpdateIocNodeRequest request);
        Task<PagedResult<IocNodeResponse>> GetAllPagedAsync(int offset, int limit, string? type = null, string? keyword = null);
        Task<bool> DeleteAsync(string id);
    }
}