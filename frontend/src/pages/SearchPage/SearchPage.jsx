import React, {useState} from 'react';
import ForceGraph2D from 'react-force-graph-2d';
import './SearchPage.css';

function SearchPage(){
  const [searchInput, setSearchInput] = useState('');
  const [searchResult, setSearchResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [graphData, setGraphData] = useState({ nodes: [], links: [] });

  const handleSearch = async (event) => {
    event.preventDefault(); 
    if(searchInput.trim() === '') {return;}
    
    setIsLoading(true);
    setSearchResult(null);
    setGraphData({ nodes: [], links: [] });
    
    try{
      const textResponse = await fetch(`https://localhost:7193/api/Search/${searchInput}`);

      if(!textResponse.ok){
        if(textResponse.status === 404){
          alert("Không tìm thấy dấu vết mã độc này trong hệ thống!");
        } else {
          alert("Lỗi Server Backend!");
        }
        setIsLoading(false);
        return;
      }

      const textData = await textResponse.json();

      // Hứng data Text (chấp mọi thể loại viết hoa/thường)
      setSearchResult({
        iocValue: textData.value || textData.Value,
        type: textData.type || textData.Type,
        riskScore: textData.riskScore || textData.RiskScore,
        country: textData.country || textData.Country || "Unknown",
        asn: textData.originRef || textData.OriginRef || "N/A",
        tags: textData.tags || textData.Tags || []
      });

      const searchKey = textData._key || textData.Key || textData.key;

      if (searchKey) {
        const graphResponse = await fetch(`https://localhost:7193/api/Graph/${searchKey}`);
        if(graphResponse.ok) {
          const realGraphData = await graphResponse.json();
          console.log("Data gốc từ C# trả về:", realGraphData);
          
          // --- BỘ LỌC ĐA VŨ TRỤ (CHẤP MỌI LOẠI CASING) ---
          const rawNodes = realGraphData.nodes || realGraphData.Nodes || [];
          const rawLinks = realGraphData.links || realGraphData.Links || [];
          const nodeMap = new Map();
          
          // 1. Chuẩn hóa Node
          rawNodes.forEach(n => {
             const id = n.id || n.Id || n._id || n.ID;
             if (id) {
                nodeMap.set(id, {
                   ...n,
                   id: id,
                   name: n.name || n.Name || n.NAME || "Unknown",
                   type: n.type || n.Type || n.TYPE || "Node",
                   val: Number(n.val || n.Val || n.VAL) || 5,
                   color: n.color || n.Color || n.COLOR || "#8b949e"
                });
             }
          });
          const safeNodes = Array.from(nodeMap.values());

          // 2. Chuẩn hóa Dây & Cắt đứt dây ảo
          const safeLinks = rawLinks.map(l => ({
             ...l,
             source: l.source || l.Source || l.SOURCE || l._from,
             target: l.target || l.Target || l.TARGET || l._to,
             name: l.name || l.Name || l.NAME || ""
          })).filter(l => 
             l.source && l.target && nodeMap.has(l.source) && nodeMap.has(l.target)
          );

          console.log("Data sau khi chuẩn hóa xong:", { nodes: safeNodes, links: safeLinks });
          setGraphData({ nodes: safeNodes, links: safeLinks });
          
        } else {
          console.warn("Node này đứng lẻ loi, không có dây mơ rễ má.");
        }
      }

    } catch(error){
      alert("Không thể kết nối đến Backend!");
      console.error(error);
    } finally{
      setIsLoading(false);
    }
  };

  return (
    <div className="search-page-wrapper">
      <div className="search-header">
        <h1 className="search-title">Tra cứu Dấu vết Tấn công (IOC)</h1>
        <p className="search-subtitle">Hệ thống phân tích thông minh</p>
        
        <form className="search-input-group" onSubmit={handleSearch}>
          <input 
            type="text" 
            className="search-input"
            placeholder="Nhập IP, Domain, hoặc Hash..." 
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)} 
          />
          <button type="submit" className="btn-search" disabled={isLoading}>
            {isLoading ? 'Đang quét...' : 'Truy vết 🔍'}
          </button>
        </form>
      </div>

      {searchResult && (
        <div className="result-container">
          
          <div className="info-panel">
            <h2 className="info-title">Chi tiết IOC</h2>
            <div className="info-row">
              <span className="info-label">Giá trị:</span>
              <strong className="info-value-large">{searchResult.iocValue}</strong>
            </div>
            <div className="info-row">
              <span className="info-label">Loại:</span>
              <span>{searchResult.type}</span>
            </div>
            <div className="info-row">
              <span className="info-label">Điểm Rủi ro:</span>
              <strong className="info-risk-high">{searchResult.riskScore} / 100</strong>
            </div>
            <div className="info-row">
              <span className="info-label">Quốc gia & Mạng:</span>
              <span>{searchResult.country} - {searchResult.asn}</span>
            </div>
            <div className="info-row tags-row">
              <span className="info-label tags-label">Nhãn dán (Tags):</span>
              <div>
                {searchResult.tags.map((tag, index) => (
                  <span key={index} className="tag-badge">{tag}</span>
                ))}
              </div>
            </div>
            <button className="btn-export" onClick={() => alert("Chức năng tải PDF đang được phát triển!")}>
              📄 Xuất Báo Cáo (PDF)
            </button>
          </div>

          <div className="graph-panel" style={{ overflow: 'hidden', position: 'relative' }}>
             
             <div style={{ position: 'absolute', top: 15, left: 15, background: 'rgba(22, 27, 34, 0.8)', padding: '10px 15px', borderRadius: 8, border: '1px solid #30363d', fontSize: 13, zIndex: 10, textAlign: 'left' }}>
                <div style={{fontWeight: 'bold', marginBottom: 8, color: '#c9d1d9'}}>Risk</div>
                <div style={{color: '#a371f7', marginBottom: 4}}>🟣 Tâm điểm</div>
                <div style={{color: '#ff7b72', marginBottom: 4}}>🔴 Nguy hiểm</div>
                <div style={{color: '#d29922', marginBottom: 4}}>🟡 Cảnh báo</div>
                <div style={{color: '#238636'}}>🟢 An toàn</div>
             </div>

             {graphData.nodes.length > 0 ? (
               <ForceGraph2D
                  graphData={graphData}
                  width={700}
                  height={500}
                  linkColor={() => 'rgba(255, 255, 255, 0.3)'}
                  linkWidth={1.5}
                  linkLabel="name" 
                  linkDirectionalArrowLength={4}
                  linkDirectionalArrowRelPos={1}

                  nodeCanvasObject={(node, ctx, globalScale) => {
                    try {
                      const x = node.x || 0;
                      const y = node.y || 0;
                      const radius = node.val + 2;
                      const label = `[${node.type}] ${node.name}`;
                      const fontSize = 12 / globalScale;
                      
                      ctx.font = `${fontSize}px Sans-Serif`;
                      ctx.beginPath();
                      ctx.arc(x, y, radius, 0, 2 * Math.PI, false);
                      ctx.fillStyle = node.color;
                      ctx.fill();
                      
                      if(node.color === '#a371f7'){
                         ctx.lineWidth = 1.5;
                         ctx.strokeStyle = 'white';
                         ctx.stroke();
                      }

                      ctx.textAlign = 'center';
                      ctx.textBaseline = 'top';
                      ctx.fillStyle = '#c9d1d9'; 
                      ctx.fillText(label, x, y + radius + 4);
                    } catch (err) {
                      console.error("Lỗi vẽ canvas:", err);
                    }
                  }}
                  
                  onNodeClick={(node) => console.log('Thông tin chi tiết Node:', node)} 
               />
             ) : (
               <div style={{color: '#8b949e', textAlign: 'center', width: '100%', marginTop: '40%'}}>
                  Đang tải đồ thị hoặc Node này chưa có mối liên hệ nào...
               </div>
             )}
          </div>

        </div>
      )}
    </div>
  );
}

export default SearchPage;