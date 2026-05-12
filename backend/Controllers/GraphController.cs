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
        // GET: api/Graph/{nodeKey}
        [HttpGet("{nodeKey}")]
        public async Task<IActionResult> GetThreatGraph(string nodeKey)
        {
            try
            {
                string query = @"
                    LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                    
                    // 1. Dùng Tâm điểm tìm xem nó nằm trong những Campaign nào
                    LET campaigns = (
                        FOR v IN 1..1 ANY startNodeId IocRelationships
                        FILTER v.Type == 'Campaign'
                        RETURN v._id
                    )
                    
                    // 2. Từ các Campaign đó, lôi tất cả các anh em (siblings) của Tâm điểm ra
                    LET siblings = (
                        FOR campId IN campaigns
                            FOR v IN 1..1 ANY campId IocRelationships
                            FILTER v.Type != 'Campaign' AND v._id != startNodeId
                            LIMIT 50 
                            RETURN v
                    )
                    
                    // 3. Lấy thêm hàng xóm cắm trực tiếp vào Tâm điểm (nếu có)
                    LET directNeighbors = (
                        FOR v IN 1..1 ANY startNodeId IocRelationships
                        FILTER v.Type != 'Campaign'
                        RETURN v
                    )
                    
                    LET allNodes = UNIQUE(APPEND(siblings, directNeighbors))
                    
                    // 4. Xử lý thuật toán Lão hóa (Decay)
                    LET nodes = (
                        FOR v IN allNodes 
                        LET daysOld = HAS(v, 'CreatedAt') ? DATE_DIFF(v.CreatedAt, DATE_NOW(), 'day') : 0
                        LET decayAmount = FLOOR(daysOld / 7) * 5
                        LET dynamicScore = MAX([0, v.RiskScore - decayAmount])
                        
                        RETURN DISTINCT {
                            id: v._id,
                            name: v.Value,
                            type: v.Type,
                            val: (dynamicScore / 10) + 1, 
                            color: dynamicScore >= 80 ? '#ff7b72' : (dynamicScore >= 50 ? '#d29922' : '#238636'),
                            actualRiskScore: dynamicScore
                        }
                    )
                    
                    LET allVisibleIds = APPEND(allNodes[*]._id, [startNodeId])
                    
                    // 5. Tơ Ngang (Links chéo giữa các Node)
                    LET realLinks = (
                        FOR e IN IocRelationships
                        FILTER e._from IN allVisibleIds AND e._to IN allVisibleIds AND e.RelationType != 'belongs_to'
                        RETURN DISTINCT {
                            source: e._from,
                            target: e._to,
                            name: e.RelationType
                        }
                    )
                    
                    // 5.2 Tơ Dọc ẢO (Nối từ tâm điểm ra anh em)
                    LET virtualLinks = (
                        FOR n IN allNodes
                        RETURN {
                            source: startNodeId,
                            target: n._id,
                            name: 'shared_campaign'
                        }
                    )
                    
                    LET links = UNIQUE(APPEND(realLinks, virtualLinks))
                    
                    // 6. Xử lý Tâm Điểm
                    LET rootNode = DOCUMENT(startNodeId)
                    LET rootDaysOld = HAS(rootNode, 'CreatedAt') ? DATE_DIFF(rootNode.CreatedAt, DATE_NOW(), 'day') : 0
                    LET rootDecay = FLOOR(rootDaysOld / 7) * 5
                    LET rootScore = MAX([0, rootNode.RiskScore - rootDecay])
                    
                    LET rootNodeFormatted = { 
                        id: rootNode._id, 
                        name: rootNode.Value, 
                        type: rootNode.Type, 
                        val: (rootScore / 10) + 3,
                        color: '#a371f7',
                        actualRiskScore: rootScore
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
                        FILTER v.Type != 'Campaign'
                        SORT v._id ASC // ĐÃ THÊM: Sắp xếp để phân trang chính xác
                        LIMIT @skip, 20 // ĐÃ ĐỔI: Lấy 20 node mỗi lần click
                        RETURN { vertex: v, edge: e }
                    )
                    
                    // ĐÃ FIX LỖI SYNTAX Ở ĐÂY & ÁP DỤNG THUẬT TOÁN DECAY
                    LET nodes = (
                        FOR p IN paths 
                        
                        // 1. Tính toán tuổi của Node (Số ngày từ lúc tạo đến hiện tại)
                        LET daysOld = HAS(p.vertex, 'CreatedAt') ? DATE_DIFF(p.vertex.CreatedAt, DATE_NOW(), 'day') : 0
                        
                        // 2. Thuật toán Decay: Cứ 7 ngày trừ 5 điểm
                        LET decayAmount = FLOOR(daysOld / 7) * 5
                        
                        // 3. Tính điểm thực tế (Dynamic Score)
                        LET dynamicScore = MAX([0, p.vertex.RiskScore - decayAmount])
                        
                        RETURN DISTINCT {
                            id: p.vertex._id,
                            name: p.vertex.Value,
                            type: p.vertex.Type,
                            
                            // Sử dụng dynamicScore để quyết định kích thước (val) và màu sắc
                            val: (dynamicScore / 10) + 1, 
                            color: dynamicScore >= 80 ? '#ff7b72' : (dynamicScore >= 50 ? '#d29922' : '#238636'),
                            
                            // Trả về thêm điểm thực tế
                            actualRiskScore: dynamicScore 
                        }
                    )
                    
                    LET allVisibleIds = APPEND(paths[*]._id, [startNodeId])
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

    // Model cho response
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
        public double? actualRiskScore { get; set; } // Bổ sung trường này cho Model C# đỡ báo lỗi
    }

    public class GraphLink
    {
        public string source { get; set; } = string.Empty;
        public string target { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }
}
