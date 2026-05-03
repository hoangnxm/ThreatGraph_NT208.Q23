using System.Collections.Generic;

namespace IocNodes.DTOs
{
    // Class hứng toàn bộ response từ AlienVault
    public class OtxPulseResponse
    {
        public List<OtxPulse> Results { get; set; } = new();
    }

    // Một Pulse giống như một bài báo cáo về 1 chiến dịch tấn công
    public class OtxPulse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<OtxIndicator> Indicators { get; set; } = new();
    }

    // Indicator chính là các IOC (IP, Domain, Hash)
    public class OtxIndicator
    {
        public string Indicator { get; set; } = string.Empty; // Giá trị (VD: 8.8.8.8)
        public string Type { get; set; } = string.Empty;      // Loại (VD: IPv4, hostname)
        public string Description { get; set; } = string.Empty;
    }
}