using ArangoDBNetStandard;
using ArangoDBNetStandard.CollectionApi.Models;
using ArangoDBNetStandard.CursorApi.Models;
using ArangoDBNetStandard.DatabaseApi.Models;
using ArangoDBNetStandard.Transport.Http;
using System.Reflection.Metadata;

namespace NT208_Project.Services
{
    public class DatabaseInitializerService : IHostedService
    {
        private readonly IArangoDBClient dbClient;
        private readonly ILogger<DatabaseInitializerService> logger;
        private readonly IConfiguration configuration;

        public DatabaseInitializerService(IArangoDBClient DbClient, ILogger<DatabaseInitializerService> Logger, IConfiguration Configuration)
        {
            dbClient = DbClient;
            logger = Logger;
            configuration = Configuration;
        }

        private async Task SeedAdminUserAsync()
        {
            try
            {
                string adminUser = configuration["ArangoAdminSettings:DefaultAdminUser"] ?? "admin";
                string adminPass = configuration["ArangoAdminSettings:DefaultAdminPassword"] ?? "admin123";

                // Kiểm tra database đã có Admin chưa
                var cursor = await dbClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody
                {
                    Query = "FOR u IN Users FILTER u.username == @username RETURN u",
                    BindVars = new Dictionary<string, object> { { "username", adminUser } }
                });

                if (!cursor.Result.Any())
                {
                    logger.LogInformation("Đang tạo tài khoản Admin mặc định...");
                    // Băm pass
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPass);
                    // Lưu vào DB
                    await dbClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody
                    {
                        Query = "INSERT { username: @username, password: @pass, role: 'Admin', isLocked: false} INTO Users",
                        BindVars = new Dictionary<string, object>
                        {
                            { "username", adminUser },
                            { "pass", hashedPassword }
                        }
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

            // Khởi tạo DB
            string databasename = configuration["ArangoDB:Database"] ?? "TIP";
            try
            {
                string dbUri = configuration["ArangoAdminSettings:ServerUri"] ?? "http://localhost:8529";
                string dbUser = configuration["ArangoAdminSettings:RootUser"] ?? "root";
                string dbPass = configuration["ArangoAdminSettings:RootPassword"] ?? "";
                using (var transport = HttpApiTransport.UsingBasicAuth(new Uri(dbUri), "_system", dbUser, dbPass))
                using (var systemClient = new ArangoDBClient(transport))
                {
                    await systemClient.Database.PostDatabaseAsync(new PostDatabaseBody
                    {
                        Name = databasename
                    });
                    logger.LogInformation($"Tạo mới thành công Database '{databasename}'!");
                }
            }
            catch (ApiErrorException ex) when (ex.ApiError.ErrorNum == 1207)
            {
                // Database đã tồn tại
                logger.LogInformation($"Database đã tồn tại. Bỏ qua!");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Lỗi tạo Database: {ex.Message}");
            }

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
                    await Task.Delay(2000);
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
