using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GraphController : ControllerBase
    {
        private readonly IArangoDBClient _dbClient;

        public GraphController(IArangoDBClient dbClient)
        {
            _dbClient = dbClient;
        }

        // GET: api/Graph/{nodeKey}
        // Gọi lần đầu khi user bấm nút "Truy vết" (Chỉ load 1 tầng)
        [HttpGet("{nodeKey}")]
        public async Task<IActionResult> GetThreatGraph(string nodeKey)
        {
            try
            {
                string query = @"
                    LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                    
                    LET paths = (
                        FOR v, e, p IN 1..1 ANY startNodeId IocRelationships 
                        LIMIT 50 // Giới hạn 50 node để tránh nổ UI ngay từ đầu
                        RETURN { vertex: v, edge: e }
                    )
                    
                    LET nodes = (
                        FOR p IN paths 
                        RETURN DISTINCT {
                            id: p.vertex._id,
                            name: p.vertex.Value,
                            type: p.vertex.Type,
                            val: (p.vertex.RiskScore / 10) + 1, 
                            color: p.vertex.RiskScore >= 80 ? '#ff7b72' : (p.vertex.RiskScore >= 50 ? '#d29922' : '#238636')
                        }
                    )
                    
                    LET links = (
                        FOR p IN paths 
                        FILTER p.edge != null 
                        RETURN DISTINCT {
                            source: p.edge._from,
                            target: p.edge._to,
                            name: p.edge.RelationType
                        }
                    )
                    
                    LET rootNode = DOCUMENT(startNodeId)
                    LET rootNodeFormatted = { 
                        id: rootNode._id, 
                        name: rootNode.Value, 
                        type: rootNode.Type, 
                        val: (rootNode.RiskScore / 10) + 3, 
                        color: '#a371f7'
                    }
                    
                    RETURN { 
                        nodes: APPEND(nodes, [rootNodeFormatted], true), 
                        links: links 
                    }";

                var bindVars = new Dictionary<string, object> { { "nodeKey", nodeKey } };
                var response = await _dbClient.Cursor.PostCursorAsync<GraphDataResponse>(
                    new PostCursorBody { Query = query, BindVars = bindVars }
                );

                var graphData = response.Result.FirstOrDefault();
                if (graphData == null) return NotFound(new { message = "Không tìm thấy dữ liệu mạng nhện!" });

                return Ok(graphData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi quét Graph", error = ex.Message });
            }
        }

        // GET: api/Graph/expand/{nodeKey}
        // Gọi khi user Click CHUỘT PHẢI vào 1 node bất kỳ trên màn hình
        [HttpGet("expand/{nodeKey}")]
        public async Task<IActionResult> ExpandGraph(string nodeKey, [FromQuery] int skip = 0)
        {
            try
            {
                string query = @"
                    LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                    
                    LET paths = (
                        FOR v, e, p IN 1..1 ANY startNodeId IocRelationships 
                        LIMIT @skip, 30 // Bỏ qua 'skip' node, lấy 30 node tiếp theo
                        RETURN { vertex: v, edge: e }
                    )
                    
                    LET nodes = (
                        FOR p IN paths 
                        RETURN DISTINCT {
                            id: p.vertex._id,
                            name: p.vertex.Value,
                            type: p.vertex.Type,
                            val: (p.vertex.RiskScore / 10) + 1, 
                            color: p.vertex.RiskScore >= 80 ? '#ff7b72' : (p.vertex.RiskScore >= 50 ? '#d29922' : '#238636')
                       LET nodes = (
                        FOR p IN paths 
                        
                        // 1. Tính toán tuổi của Node (Số ngày từ lúc tạo đến hiện tại)
                        LET daysOld = DATE_DIFF(p.vertex.CreatedAt, DATE_NOW(), 'day')
                        
                        // 2. Thuật toán Decay: Cứ 7 ngày trừ 5 điểm
                        LET decayAmount = FLOOR(daysOld / 7) * 5
                        
                        // 3. Tính điểm thực tế (Dynamic Score)
                        LET dynamicScore = MAX([0, p.vertex.RiskScore - decayAmount])
                        
                        RETURN DISTINCT {
                            id: p.vertex._id,
                            name: p.vertex.Value,
                            type: p.vertex.Type,
                            
                            // Sử dụng dynamicScore để quyết định kích thước và màu sắc
                            val: (dynamicScore / 10) + 1, 
                            color: dynamicScore >= 80 ? '#ff7b72' : (dynamicScore >= 50 ? '#d29922' : '#238636'),
                            
                            // Trả về thêm điểm thực tế để nếu cần hiển thị lên UI thì dùng
                            actualRiskScore: dynamicScore 
                        }
                    ) }
                    )
                    
                    LET links = (
                        FOR p IN paths 
                        FILTER p.edge != null 
                        RETURN DISTINCT {
                            source: p.edge._from,
                            target: p.edge._to,
                            name: p.edge.RelationType
                        }
                    )
                    
                    RETURN { 
                        nodes: nodes, 
                        links: links 
                    }";

                // Truyền tham số skip vào AQL
                var bindVars = new Dictionary<string, object> {
                    { "nodeKey", nodeKey },
                    { "skip", skip }
                };

                var response = await _dbClient.Cursor.PostCursorAsync<GraphDataResponse>(
                    new PostCursorBody { Query = query, BindVars = bindVars }
                );

                var graphData = response.Result.FirstOrDefault();
                if (graphData == null) return NotFound(new { message = "Không có data!" });

                return Ok(graphData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi mở rộng Graph", error = ex.Message });
            }
        }
    }

    // Model cho response (Giữ nguyên)
    public class GraphDataResponse
    {
        public List<GraphNode> nodes { get; set; } = new List<GraphNode>();
        public List<GraphLink> links { get; set; } = new List<GraphLink>();
    }

    public class GraphNode
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public double val { get; set; }
        public string color { get; set; } = string.Empty;
    }

    public class GraphLink
    {
        public string source { get; set; } = string.Empty;
        public string target { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }
}