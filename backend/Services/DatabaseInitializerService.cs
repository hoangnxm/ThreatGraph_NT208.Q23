using ArangoDBNetStandard;
using ArangoDBNetStandard.CollectionApi.Models;
using ArangoDBNetStandard.CursorApi.Models;
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

        private async Task SeedAdminUserAsync()
        {
            try
            {
                // Kiểm tra database đã có Admin chưa
                var cursor = await dbClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody
                {
                    Query = "FOR u IN Users FILTER u.username == 'admin' RETURN u"
                });

                if (!cursor.Result.Any())
                {
                    logger.LogInformation("Đang tạo tài khoản Admin mặc định...");
                    // Băm pass
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin123");
                    // Lưu vào DB
                    await dbClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody
                    {
                        Query = "INSERT { username: 'admin', password: @pass, role: 'Admin', isLocked: false} INTO Users",
                        BindVars = new Dictionary<string, object> { { "pass", hashedPassword } }
                    });
                    logger.LogInformation("Tạo tài khoản thành công!");
                }
            }
            catch (Exception ex)
            {

                logger.LogError($"Lỗi khi tạo User: {ex.Message}");
            }
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

            await SeedAdminUserAsync();

            logger.LogInformation("HOÀN TẤT SETUP DATABASE!");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
