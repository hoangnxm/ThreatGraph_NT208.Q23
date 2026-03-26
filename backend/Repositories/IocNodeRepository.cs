using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using IocNodes.Models;

namespace IocNodes.Repositories
{
    public class IocNodeRepository : IIocNodeRepository
    {
        private readonly IArangoDBClient _dbClient;
        private const string CollectionName = "IocNodes";

        public IocNodeRepository(IArangoDBClient dbClient)
        {
            _dbClient = dbClient;
        }

        public async Task<IocNode?> GetByIdAsync(string key)
        {
            var query = "RETURN DOCUMENT(@collection, @key)";
            var bindVars = new Dictionary<string, object>
            {
                { "collection", CollectionName },
                { "key", key }
            };

            var result = await _dbClient.Cursor.PostCursorAsync<IocNode>(
                new PostCursorBody { Query = query, BindVars = bindVars }
            );

            return result.Result.FirstOrDefault();
        }

        public async Task<IEnumerable<IocNode>> GetAllAsync(int offset, int limit)
        {
            var query = @"
                FOR i IN @@collection
                SORT i.CreatedAt DESC
                LIMIT @offset, @limit
                RETURN i";

            var bindVars = new Dictionary<string, object>
            {
                { "@collection", CollectionName },
                { "offset", offset },
                { "limit", limit }
            };

            var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(
                new PostCursorBody { Query = query, BindVars = bindVars }
            );

            return response.Result;
        }

        public async Task<IocNode> CreateAsync(IocNode node)
        {
            var query = @"
                INSERT @node INTO @@collection
                RETURN NEW";

            var bindVars = new Dictionary<string, object>
            {
                { "@collection", CollectionName },
                { "node", node }
            };

            var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(
                new PostCursorBody { Query = query, BindVars = bindVars }
            );

            return response.Result.First();
        }

        public async Task<IocNode?> UpdateAsync(string key, IocNode node)
        {
            var query = @"
                UPDATE @key WITH @node IN @@collection
                RETURN NEW";

            var bindVars = new Dictionary<string, object>
            {
                { "@collection", CollectionName },
                { "key", key },
                { "node", node }
            };

            try
            {
                var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(
                    new PostCursorBody { Query = query, BindVars = bindVars }
                );
                return response.Result.FirstOrDefault();
            }
            catch (ApiErrorException ex) when (ex.ApiError.ErrorNum == 1202)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var query = @"
                REMOVE @key IN @@collection
                RETURN OLD";

            var bindVars = new Dictionary<string, object>
            {
                { "@collection", CollectionName },
                { "key", key }
            };

            try
            {
                var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(
                    new PostCursorBody { Query = query, BindVars = bindVars }
                );
                return response.Result.Any();
            }
            catch (ApiErrorException ex) when (ex.ApiError.ErrorNum == 1202)
            {
                return false;
            }
        }
    }
}