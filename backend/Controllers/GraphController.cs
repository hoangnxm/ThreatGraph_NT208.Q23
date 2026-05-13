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

        [HttpGet("{nodeKey}")]
        public async Task<IActionResult> GetThreatGraph(string nodeKey)
        {
            try
            {
                string query = @"
                    LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                    
                    LET campaigns = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type == 'Campaign' RETURN v._id)
                    
                    LET siblings = (
                        FOR campId IN campaigns
                            FOR v IN 1..1 ANY campId IocRelationships
                            FILTER v.Type != 'Campaign' AND v._id != startNodeId
                            LIMIT 50 
                            RETURN v
                    )
                    
                    LET directNeighbors = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type != 'Campaign' RETURN v)
                    
                    LET allNodes = UNIQUE(APPEND(siblings, directNeighbors))
                    
                    // Lấy danh sách ID của tất cả Node sẽ hiển thị để làm mốc so sánh
                    LET allVisibleIds = APPEND(allNodes[*]._id, [startNodeId])
                    
                    LET nodes = (
                        FOR v IN allNodes 
                        LET daysOld = HAS(v, 'CreatedAt') ? DATE_DIFF(v.CreatedAt, DATE_NOW(), 'day') : 0
                        LET decayAmount = FLOOR(daysOld / 7) * 5
                        LET dynamicScore = MAX([0, v.RiskScore - decayAmount])
                        
                        // Kiểm tra xem Node này có Edge nào nối ra 'người lạ' (không nằm trong allVisibleIds) hay không
                        LET hiddenEdges = (
                            FOR e IN IocRelationships
                            FILTER (e._from == v._id AND e._to NOT IN allVisibleIds) OR (e._to == v._id AND e._from NOT IN allVisibleIds)
                            LIMIT 1 RETURN 1
                        )
                        
                        RETURN DISTINCT {
                            id: v._id,
                            name: v.Value,
                            type: v.Type,
                            val: (dynamicScore / 10) + 1, 
                            color: dynamicScore >= 80 ? '#ff7b72' : (dynamicScore >= 50 ? '#d29922' : '#238636'),
                            actualRiskScore: dynamicScore,
                            isExpandable: LENGTH(hiddenEdges) > 0 // Trả về true nếu còn lấp ló Edge ẩn
                        }
                    )
                    
                    LET realLinks = (FOR e IN IocRelationships FILTER e._from IN allVisibleIds AND e._to IN allVisibleIds AND e.RelationType != 'belongs_to' RETURN DISTINCT { source: e._from, target: e._to, name: e.RelationType })
                    LET virtualLinks = (FOR n IN allNodes RETURN { source: startNodeId, target: n._id, name: 'shared_campaign' })
                    LET links = UNIQUE(APPEND(realLinks, virtualLinks))
                    
                    LET rootNode = DOCUMENT(startNodeId)
                    LET rootDaysOld = HAS(rootNode, 'CreatedAt') ? DATE_DIFF(rootNode.CreatedAt, DATE_NOW(), 'day') : 0
                    LET rootScore = MAX([0, rootNode.RiskScore - (FLOOR(rootDaysOld / 7) * 5)])
                    
                    // Kiểm tra riêng cho Tâm Điểm
                    LET rootHiddenEdges = (
                        FOR e IN IocRelationships
                        FILTER (e._from == rootNode._id AND e._to NOT IN allVisibleIds) OR (e._to == rootNode._id AND e._from NOT IN allVisibleIds)
                        LIMIT 1 RETURN 1
                    )
                    
                    LET rootNodeFormatted = { 
                        id: rootNode._id, 
                        name: rootNode.Value, 
                        type: rootNode.Type, 
                        val: (rootScore / 10) + 3,
                        color: '#a371f7',
                        actualRiskScore: rootScore,
                        isExpandable: LENGTH(rootHiddenEdges) > 0
                    }
                    
                    RETURN { nodes: APPEND(nodes, [rootNodeFormatted], true), links: links }";

                var bindVars = new Dictionary<string, object> { { "nodeKey", nodeKey } };
                var response = await _dbClient.Cursor.PostCursorAsync<GraphDataResponse>(new PostCursorBody { Query = query, BindVars = bindVars });
                var graphData = response.Result.FirstOrDefault();
                if (graphData == null) return NotFound(new { message = "Không tìm thấy dữ liệu mạng nhện!" });

                return Ok(graphData);
            }
            catch (Exception ex) { return StatusCode(500, new { message = "Lỗi khi quét Graph", error = ex.Message }); }
        }

        [HttpGet("expand/{nodeKey}")]
        public async Task<IActionResult> ExpandGraph(string nodeKey, [FromQuery] int skip = 0)
        {
            try
            {
                string query = @"
                    LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                    LET campaigns = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type == 'Campaign' RETURN v._id)
                    LET siblings = (FOR campId IN campaigns FOR v IN 1..1 ANY campId IocRelationships FILTER v.Type != 'Campaign' AND v._id != startNodeId RETURN v)
                    LET directNeighbors = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type != 'Campaign' RETURN v)
                    
                    LET combined = UNIQUE(APPEND(siblings, directNeighbors))
                    LET allNodes = (FOR v IN combined SORT v._id ASC LIMIT @skip, 20 RETURN v)
                    LET allVisibleIds = APPEND(allNodes[*]._id, [startNodeId])
                    
                    LET nodes = (
                        FOR v IN allNodes 
                        LET daysOld = HAS(v, 'CreatedAt') ? DATE_DIFF(v.CreatedAt, DATE_NOW(), 'day') : 0
                        LET dynamicScore = MAX([0, v.RiskScore - (FLOOR(daysOld / 7) * 5)])
                        
                        LET hiddenEdges = (
                            FOR e IN IocRelationships
                            FILTER (e._from == v._id AND e._to NOT IN allVisibleIds) OR (e._to == v._id AND e._from NOT IN allVisibleIds)
                            LIMIT 1 RETURN 1
                        )
                        
                        RETURN DISTINCT {
                            id: v._id,
                            name: v.Value,
                            type: v.Type,
                            val: (dynamicScore / 10) + 1, 
                            color: dynamicScore >= 80 ? '#ff7b72' : (dynamicScore >= 50 ? '#d29922' : '#238636'),
                            actualRiskScore: dynamicScore,
                            isExpandable: LENGTH(hiddenEdges) > 0
                        }
                    )
                    
                    LET realLinks = (FOR e IN IocRelationships FILTER e._from IN allVisibleIds AND e._to IN allVisibleIds AND e.RelationType != 'belongs_to' RETURN DISTINCT { source: e._from, target: e._to, name: e.RelationType })
                    LET virtualLinks = (FOR n IN allNodes RETURN { source: startNodeId, target: n._id, name: 'shared_campaign' })
                    
                    RETURN { nodes: nodes, links: UNIQUE(APPEND(realLinks, virtualLinks)) }";

                var bindVars = new Dictionary<string, object> { { "nodeKey", nodeKey }, { "skip", skip } };
                var response = await _dbClient.Cursor.PostCursorAsync<GraphDataResponse>(new PostCursorBody { Query = query, BindVars = bindVars });
                var graphData = response.Result.FirstOrDefault();
                if (graphData == null) return NotFound(new { message = "Không có data!" });

                return Ok(graphData);
            }
            catch (Exception ex) { return StatusCode(500, new { message = "Lỗi khi mở rộng Graph", error = ex.Message }); }
        }
    }

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
        public double? actualRiskScore { get; set; }

        // Thêm trường cờ báo hiệu mở rộng
        public bool isExpandable { get; set; }
    }

    public class GraphLink
    {
        public string source { get; set; } = string.Empty;
        public string target { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }
}