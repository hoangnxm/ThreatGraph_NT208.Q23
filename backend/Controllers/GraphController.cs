using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace NT208_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphController : ControllerBase
    {
        private readonly IArangoDBClient _dbClient;

        public GraphController(IArangoDBClient dbClient)
        {
            _dbClient = dbClient;
        }

        // GET: api/Graph/{nodeKey}
        [HttpGet("{nodeKey}")]
        public async Task<IActionResult> GetThreatGraph(string nodeKey)
        {
            try
            {
                string query = @"
                    LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                    
                    LET paths = (
                        FOR v, e, p IN 1..2 ANY startNodeId IocRelationships 
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

                var bindVars = new Dictionary<string, object>
                {
                    { "nodeKey", nodeKey }
                };

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
    }

    public class GraphLink
    {
        public string source { get; set; } = string.Empty;
        public string target { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }
}