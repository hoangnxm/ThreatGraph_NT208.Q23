using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IocNodes.DTOs;
using backend.Models;
using backend.Repositories;

namespace IocNodes.Services
{
    public class IocNodeService : IIocNodeService
    {
        private readonly IIocNodeRepository _repository;

        public IocNodeService(IIocNodeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IocNodeResponse?> GetByIdAsync(string id)
        {
            var node = await _repository.GetByIdAsync(id);
            if (node == null) return null;
            return MapToResponse(node);
        }

        public async Task<IocNodeResponse?> GetByValueAsync(string value)
        {
            // Gọi xuống Repository để truy vấn DB
            var node = await _repository.GetByValueAsync(value);

            // Nếu không tìm thấy thì trả về null
            if (node == null) return null;

            // Nếu tìm thấy, chuyển đổi Model thành DTO
            return MapToResponse(node);
        }

        public async Task<IEnumerable<IocNodeResponse>> GetAllAsync(int offset, int limit)
        {
            var nodes = await _repository.GetAllAsync(offset, limit);
            return nodes.Select(MapToResponse);
        }

        public async Task<PagedResult<IocNodeResponse>> GetAllPagedAsync(int offset, int limit, string? type = null, string? keyword = null)
        {
            // Lấy danh sách items đã được lọc và phân trang từ DB
            var nodes = await _repository.GetAllAsync(offset, limit, type, keyword);

            // Lấy tổng số lượng bản ghi THỰC TẾ sau khi lọc để vẽ thanh phân trang
            var totalCount = await _repository.GetCountAsync(type, keyword);

            return new PagedResult<IocNodeResponse>
            {
                Items = nodes.Select(MapToResponse),
                TotalCount = totalCount,
                Page = (offset / limit) + 1,
                Limit = limit
            };
        }
        public async Task<IocNodeResponse> CreateAsync(CreateIocNodeRequest request)
        {
            var node = new IocNode
            {
                Type = request.Type,
                Value = request.Value.Trim(),
                RiskScore = request.RiskScore,
                Country = request.Country,
                Tags = request.Tags ?? new List<string>(),
                OriginRef = request.OriginRef,
                CreatedAt = DateTime.UtcNow 
            };

            var createdNode = await _repository.CreateAsync(node);
            return MapToResponse(createdNode);
        }

        public async Task<IocNodeResponse?> UpdateAsync(string id, UpdateIocNodeRequest request)
        {
            var existingNode = await _repository.GetByIdAsync(id);
            if (existingNode == null) return null;

            if (request.RiskScore.HasValue) 
                existingNode.RiskScore = request.RiskScore.Value;
            
            if (request.Country != null) 
                existingNode.Country = request.Country;
            
            if (request.Tags != null) 
            {
                existingNode.Tags = existingNode.Tags.Concat(request.Tags).Distinct().ToList();
            }

            if (request.OriginRef != null)
                existingNode.OriginRef = request.OriginRef;

            existingNode.UpdatedAt = DateTime.UtcNow;

            var updatedNode = await _repository.UpdateAsync(id, existingNode);
            return updatedNode != null ? MapToResponse(updatedNode) : null;
        }
        public async Task<bool> DeleteAsync(string id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> DeleteAllAsync()
        {
            try
            {
                return await _repository.DeleteAllIocsAsync();
            }
            catch (Exception ex)
            {
                // Có thể thêm log lỗi ở đây nếu cần
                throw new Exception($"Lỗi ở tầng Service khi xóa toàn bộ IOC: {ex.Message}");
            }
        }



        private IocNodeResponse MapToResponse(IocNode node)
        {
            return new IocNodeResponse
            {
                Id = node.Key ?? string.Empty,
                Type = node.Type,
                Value = node.Value,
                RiskScore = node.RiskScore,
                Country = node.Country,
                Tags = node.Tags ?? new List<string>(),
                OriginRef = node.OriginRef,
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt 
            };
        }

        public async Task<bool> CreateRelationshipAsync(CreateRelationshipRequest request)
        {
            // Đi tìm Node nguồn
            var fromNode = await _repository.GetByValueAsync(request.FromValue.Trim());
            if (fromNode == null || string.IsNullOrEmpty(fromNode.Key))
            {
                throw new Exception($"Không tìm thấy Node nguồn với giá trị: {request.FromValue}. Vui lòng thêm IOC này vào hệ thống trước.");
            }

            // Đi tìm Node đích
            var toNode = await _repository.GetByValueAsync(request.ToValue.Trim());
            if (toNode == null || string.IsNullOrEmpty(toNode.Key))
            {
                throw new Exception($"Không tìm thấy Node đích với giá trị: {request.ToValue}. Vui lòng thêm IOC này vào hệ thống trước.");
            }

            // Đã có đủ 2 Key, tiến hành nối như bình thường
            return await _repository.CreateRelationshipAsync(
                fromNode.Key,
                toNode.Key,
                request.RelationType,
                "Manual"
            );
        }
    }
}