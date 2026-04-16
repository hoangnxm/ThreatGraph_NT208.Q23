using System.Threading.Tasks;

namespace IocNodes.Services
{
    public interface IDataFeedService
    {
        /// <summary>
        /// Gọi API AlienVault, lấy dữ liệu và lưu vào ArangoDB.
        /// Trả về số lượng IOC đã thêm thành công.
        /// </summary>
        Task<int> SyncAlienVaultDataAsync();
    }
}