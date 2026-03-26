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

        public async Task<IEnumerable<IocNodeResponse>> GetAllAsync(int offset, int limit)
        {
            var nodes = await _repository.GetAllAsync(offset, limit);
            return nodes.Select(MapToResponse);
        }

        public async Task<IocNodeResponse> CreateAsync(CreateIocNodeRequest request)
        {
            var node = new IocNode
            {
                Type = request.Type,
                Value = request.Value,
                RiskScore = request.RiskScore,
                Country = request.Country,
                Tags = request.Tags ?? new List<string>(),
                OriginRef = request.OriginRef,
                CreatedAt = DateTime.UtcNow // Luôn lưu giờ chuẩn UTC
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
                existingNode.Tags = request.Tags;

            var updatedNode = await _repository.UpdateAsync(id, existingNode);
            return updatedNode != null ? MapToResponse(updatedNode) : null;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            return await _repository.DeleteAsync(id);
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
                CreatedAt = node.CreatedAt
            };
        }
    }
}