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
        Task<bool> DeleteAsync(string id);
    }
}