using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Repositories
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
            var bindVars = new Dictionary<string, object> { { "collection", CollectionName }, { "key", key } };
            var result = await _dbClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = query, BindVars = bindVars });
            return result.Result.FirstOrDefault();
        }

        public async Task<IEnumerable<IocNode>> GetAllAsync(int offset, int limit, string? type = null, string? keyword = null)
        {
            var filterAql = "";
            var bindVars = new Dictionary<string, object> { { "@collection", CollectionName }, { "offset", offset }, { "limit", limit } };

            if (!string.IsNullOrEmpty(type)) { filterAql += " FILTER i.Type == @type "; bindVars.Add("type", type); }
            if (!string.IsNullOrEmpty(keyword))
            {
                var prefix = string.IsNullOrEmpty(type) ? "FILTER" : "AND";
                filterAql += $" {prefix} CONTAINS(LOWER(i.Value), LOWER(@keyword)) ";
                bindVars.Add("keyword", keyword);
            }

            var query = $"FOR i IN @@collection {filterAql} SORT i._key DESC LIMIT @offset, @limit RETURN i";
            var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = query, BindVars = bindVars });
            return response.Result;
        }

        public async Task<IocNode> CreateAsync(IocNode node)
        {
            var query = "INSERT @node INTO @@collection RETURN NEW";
            var bindVars = new Dictionary<string, object> { { "@collection", CollectionName }, { "node", node } };
            var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = query, BindVars = bindVars });
            return response.Result.First();
        }

        public async Task<IocNode?> UpdateAsync(string key, IocNode node)
        {
            var query = "UPDATE @key WITH @node IN @@collection RETURN NEW";
            var bindVars = new Dictionary<string, object> { { "@collection", CollectionName }, { "key", key }, { "node", node } };
            try
            {
                var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = query, BindVars = bindVars });
                return response.Result.FirstOrDefault();
            }
            catch (ApiErrorException ex) when (ex.ApiError.ErrorNum == 1202) { return null; }
        }

        public async Task<int> GetCountAsync(string? type = null, string? keyword = null)
        {
            var filterAql = "";
            var bindVars = new Dictionary<string, object> { { "@collection", CollectionName } };
            if (!string.IsNullOrEmpty(type)) { filterAql += " FILTER i.Type == @type "; bindVars.Add("type", type); }
            if (!string.IsNullOrEmpty(keyword))
            {
                var prefix = string.IsNullOrEmpty(type) ? "FILTER" : "AND";
                filterAql += $" {prefix} CONTAINS(LOWER(i.Value), LOWER(@keyword)) ";
                bindVars.Add("keyword", keyword);
            }
            var query = $"FOR i IN @@collection {filterAql} COLLECT WITH COUNT INTO length RETURN length";
            var response = await _dbClient.Cursor.PostCursorAsync<int>(new PostCursorBody { Query = query, BindVars = bindVars });
            return response.Result.First();
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var query = "REMOVE @key IN @@collection RETURN OLD";
            var bindVars = new Dictionary<string, object> { { "@collection", CollectionName }, { "key", key } };
            try
            {
                var response = await _dbClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = query, BindVars = bindVars });
                return response.Result.Any();
            }
            catch (ApiErrorException ex) when (ex.ApiError.ErrorNum == 1202) { return false; }
        }

        public async Task<IocNode?> GetByValueAsync(string value)
        {
            var query = "FOR i IN @@collection FILTER i.Value == @value RETURN i";
            var bindVars = new Dictionary<string, object> { { "@collection", CollectionName }, { "value", value } };
            var result = await _dbClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody { Query = query, BindVars = bindVars });
            return result.Result.FirstOrDefault();
        }

        public async Task<bool> CreateRelationshipAsync(string fromKey, string toKey, string relationType, string originRef)
        {
            var query = @"
                INSERT {
                    _from: @from,
                    _to: @to,
                    RelationType: @relationType,
                    OriginRef: @originRef
                } INTO IocRelationships";

            var bindVars = new Dictionary<string, object> {
                { "from", $"IocNodes/{fromKey}" },
                { "to", $"IocNodes/{toKey}" },
                { "relationType", relationType },
                { "originRef", originRef }
            };

            try
            {
                await _dbClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody { Query = query, BindVars = bindVars });
                return true;
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Lỗi Database khi tạo Edge: {ex.Message}");
            }
        }

        // Thêm vào IocNodeRepository.cs
        public async Task<bool> DeleteAllIocsAsync()
        {
            // Query xóa sạch node và quan hệ
            var query = @"
        FOR i IN IocNodes REMOVE i IN IocNodes
        LET relations = (FOR r IN IocRelationships REMOVE r IN IocRelationships)
        RETURN true";

            try
            {
                await _dbClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody { Query = query });
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi dọn dẹp Database: {ex.Message}");
            }
        }
    }
}