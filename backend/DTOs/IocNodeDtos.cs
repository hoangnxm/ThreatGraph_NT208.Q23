using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IocNodes.DTOs
{
    // ---------------------------------------------------------------------------
    // Request DTOs
    // ---------------------------------------------------------------------------

    /// <summary>Payload for POST /api/ioc-nodes</summary>
    public class CreateIocNodeRequest
    {
        [Required]
        [RegularExpression("^(IP|Domain|Hash)$", ErrorMessage = "type must be 'IP', 'Domain', or 'Hash'.")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string Value { get; set; } = string.Empty;

        [Range(0, 100)]
        public int RiskScore { get; set; } = 0;

        [StringLength(2, MinimumLength = 2, ErrorMessage = "country must be a 2-letter ISO code.")]
        public string? Country { get; set; }

        public List<string>? Tags { get; set; }

        [Required]
        public string OriginRef { get; set; } = string.Empty;
    }

    /// <summary>Payload for PUT /api/ioc-nodes/{id} — type and value are intentionally absent.</summary>
    public class UpdateIocNodeRequest
    {
        [Range(0, 100)]
        public int? RiskScore { get; set; }

        [StringLength(2, MinimumLength = 2, ErrorMessage = "country must be a 2-letter ISO code.")]
        public string? Country { get; set; }

        public List<string>? Tags { get; set; }
    }

    // ---------------------------------------------------------------------------
    // Response DTO
    // ---------------------------------------------------------------------------

    /// <summary>Outbound representation of an IocNode.</summary>
    public class IocNodeResponse
    {
        public string Id { get; set; } = string.Empty;   // _key
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public string? Country { get; set; }
        public List<string> Tags { get; set; } = new();
        public string OriginRef { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ---------------------------------------------------------------------------
    // Pagination wrapper
    // ---------------------------------------------------------------------------

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int Limit { get; set; }
        public long TotalCount { get; set; }
    }
}
