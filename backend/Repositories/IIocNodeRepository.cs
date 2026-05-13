using backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Repositories
{
    public interface IIocNodeRepository
    {
        // Các hàm cũ của bạn...
        Task<IocNode?> GetByIdAsync(string key);
        Task<IEnumerable<IocNode>> GetAllAsync(int offset, int limit, string? type = null, string? keyword = null);
        Task<IocNode> CreateAsync(IocNode node);
        Task<IocNode?> UpdateAsync(string key, IocNode node);
        Task<int> GetCountAsync(string? type = null, string? keyword = null);
        Task<bool> DeleteAsync(string key);
        Task<IocNode?> GetByValueAsync(string value);
        Task<bool> CreateRelationshipAsync(string fromKey, string toKey, string relationType, string originRef);
        Task<bool> DeleteAllIocsAsync();

        // THÊM 2 HÀM MỚI NÀY
        Task BulkUpsertNodesAsync(List<IocNode> nodes);
        Task BulkInsertEdgesAsync(List<dynamic> edges);
    }
}