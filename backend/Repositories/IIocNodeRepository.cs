using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories
{
    public interface IIocNodeRepository
    {
        Task<IocNode?> GetByIdAsync(string key);
        Task<IEnumerable<IocNode>> GetAllAsync(int offset, int limit);
        Task<IocNode> CreateAsync(IocNode node);
        Task<IocNode?> UpdateAsync(string key, IocNode node);
        Task<bool> DeleteAsync(string key);
    }
}