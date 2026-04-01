using ArangoDBNetStandard;
using ArangoDBNetStandard.CollectionApi.Models;
using System.Reflection.Metadata;

namespace NT208_Project.Services
{
    public class DatabaseInitializerService : IHostedService
    {
        private readonly IArangoDBClient dbClient;
        private readonly ILogger<DatabaseInitializerService> logger;

        public DatabaseInitializerService(IArangoDBClient DbClient, ILogger<DatabaseInitializerService> Logger)
        {
            dbClient = DbClient;
            logger = Logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Checking And Creating Database....");

            // Khởi tạo các collection
            var collections = new List<(string Name, CollectionType Type)>
            {
                ("Users", CollectionType.Document),
                ("DataFeeds", CollectionType.Document),
                ("AuditLogs", CollectionType.Document),
                ("IocNodes", CollectionType.Document),
                ("IocRelationships", CollectionType.Edge)
            };

            foreach (var col in collections)
            {
                try
                {
                    await dbClient.Collection.PostCollectionAsync(
                        new PostCollectionBody { Name = col.Name, Type = col.Type }
                    );

                    logger.LogInformation($"Tạo mới thành công bảng {col.Name}");
                }
                catch (ApiErrorException ex) when (ex.ApiError.ErrorNum == 1207)
                {
                    // Mã 1207 là mã báo bảng này đã tồn tại
                    logger.LogInformation($"Bảng {col.Name} đã tồn tại. Bỏ qua!!");
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"Lỗi khi tạo bảng {col.Name}: {ex.Message}");
                }
            }
            logger.LogInformation("HOÀN TẤT SETUP DATABASE!");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
