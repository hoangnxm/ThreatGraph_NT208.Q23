using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories
{
    public interface IIocNodeRepository
    {
        Task<IocNode?> GetByIdAsync(string key);
        Task<IEnumerable<IocNode>> GetAllAsync(int offset, int limit, string? type = null, string? keyword = null);
        Task<IocNode> CreateAsync(IocNode node);
        Task<IocNode?> UpdateAsync(string key, IocNode node);
        Task<int> GetCountAsync(string? type = null, string? keyword = null);
        Task<bool> DeleteAsync(string key);

        Task<bool> DeleteAllIocsAsync();

        // Khai báo 2 hàm xử lý Đồ thị
        Task<IocNode?> GetByValueAsync(string value);
        Task<bool> CreateRelationshipAsync(string fromKey, string toKey, string relationType, string originRef);
    }
}