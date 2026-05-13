import React, {useState, useCallback, useRef, useEffect} from 'react';
import ForceGraph2D from 'react-force-graph-2d';
import axiosClient from '../../api/axiosClient';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';
import './SearchPage.css';

function SearchPage(){
  const [searchInput, setSearchInput] = useState('');
  const [searchResult, setSearchResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [graphData, setGraphData] = useState({ nodes: [], links: [] });
  const [isExporting, setIsExporting] = useState(false);
  const [selectedNode, setSelectedNode] = useState(null);
  const [hoverNode, setHoverNode] = useState(null);

  const expandStatsRef = useRef({});
  const fgRef = useRef();

  useEffect(() => {
    if (fgRef.current) {
      fgRef.current.d3Force('charge').strength(-800);
      fgRef.current.d3Force('link').distance(150);
      fgRef.current.d3ReheatSimulation();
    }
  }, [graphData]);

  const handleSearch = async (event) => {
    event.preventDefault(); 
    if(searchInput.trim() === '') { return; }
    
    setIsLoading(true);
    setSearchResult(null);
    setSelectedNode(null); 
    setGraphData({ nodes: [], links: [] });
    
    try {
      const textResponse = await axiosClient.get(`/Search/${searchInput}`);
      const textData = textResponse.data; 

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
        const graphResponse = await axiosClient.get(`/Graph/${searchKey}`);
        const realGraphData = graphResponse.data;
        
        const rawNodes = realGraphData.nodes || realGraphData.Nodes || [];
        const rawLinks = realGraphData.links || realGraphData.Links || [];
        const nodeMap = new Map();
        
        rawNodes.forEach(n => {
           const id = n.id || n.Id || n._id || n.ID;
           const type = n.type || n.Type || n.TYPE || "Node";
           let color = n.color || n.Color || n.COLOR || "#8b949e";

           if (type.toLowerCase() === 'domain' && color !== '#a371f7') {
               color = '#58a6ff'; 
           }

           if (id) {
              nodeMap.set(id, {
                 ...n,
                 id: id,
                 name: n.name || n.Name || n.NAME || "Unknown",
                 type: type,
                 val: Number(n.val || n.Val || n.VAL) || 5,
                 color: color,
                 // Đọc cờ isExpandable từ Backend
                 isExpandable: n.isExpandable || false 
              });
           }
        });

        const safeNodes = Array.from(nodeMap.values());
        const safeLinks = rawLinks.map(l => ({
           ...l,
           source: l.source || l.Source || l.SOURCE || l._from,
           target: l.target || l.Target || l.TARGET || l._to,
           name: l.name || l.Name || l.NAME || ""
        })).filter(l => l.source && l.target && nodeMap.has(l.source) && nodeMap.has(l.target));

        setGraphData({ nodes: safeNodes, links: safeLinks });

        expandStatsRef.current = {}; 
        expandStatsRef.current[searchKey] = 50; 
      }
    } catch(error) {
      if (error.response && error.response.status === 404) {
        alert("Không tìm thấy dấu vết mã độc này!");
      } else {
        console.error("Lỗi kết nối:", error);
        alert("Lỗi kết nối đến Backend! Kiểm tra lại cấu hình.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleNodeRightClick = useCallback(async (node) => {
    const nodeKey = String(node.id).includes('/') ? node.id.split('/')[1] : node.id;
    const currentSkip = expandStatsRef.current[nodeKey] || 0;

    try {
      const response = await axiosClient.get(`/Graph/expand/${nodeKey}?skip=${currentSkip}`);
      const expandedData = response.data;
      
      const newNodes = expandedData.nodes || expandedData.Nodes || [];
      const newLinks = expandedData.links || expandedData.Links || [];

      if (newNodes.length === 0) {
        alert("Node này đã hiển thị hết toàn bộ dữ liệu liên quan!");
        // Nếu click mở rộng mà server trả về rỗng, tắt luôn huy hiệu dấu +
        setGraphData(prevData => ({
          nodes: prevData.nodes.map(n => 
            n.id === node.id ? { ...n, isFullyExpanded: true } : n
          ),
          links: prevData.links
        }));
        return;
      }

      expandStatsRef.current[nodeKey] = currentSkip + 20;

      setGraphData(prevData => {
        const existingNodeIds = new Set(prevData.nodes.map(n => n.id));
        
        // Cập nhật lại Node vừa click xem nó còn khả năng Expand không dựa theo data mới trả về
        // Server trả về danh sách newNodes không chứa Node gốc (bởi vì ta mở rộng TỪ nó ra), 
        // Do đó ta vẫn giữ nguyên logic cũ, chỉ thêm cờ isExpandable cho Node mới
        const uniqueNewNodes = newNodes
          .filter(n => n.id && !existingNodeIds.has(n.id))
          .map(n => {
            let color = n.color || "#8b949e";
            if ((n.type || "").toLowerCase() === 'domain' && color !== '#a371f7') color = '#58a6ff';
            
            return {
              ...n,
              name: n.name || "Unknown",
              val: Number(n.val) || 5,
              color: color,
              isExpandable: n.isExpandable || false,
              x: node.x + (Math.random() - 0.5) * 50, 
              y: node.y + (Math.random() - 0.5) * 50 
            };
          });

        if(uniqueNewNodes.length === 0){
             alert("Các nhánh mở rộng đều đã có mặt trên bản đồ!");
             return prevData;
        }

        const getLinkId = (link) => {
          const sourceId = typeof link.source === 'object' ? link.source.id : link.source;
          const targetId = typeof link.target === 'object' ? link.target.id : link.target;
          return `${sourceId}->${targetId}`;
        };

        const existingLinkIds = new Set(prevData.links.map(getLinkId));
        const uniqueNewLinks = newLinks
          .map(l => ({
            ...l,
            source: l.source || l._from,
            target: l.target || l._to
          }))
          .filter(l => l.source && l.target && !existingLinkIds.has(`${l.source}->${l.target}`));

        return {
          nodes: [...prevData.nodes, ...uniqueNewNodes],
          links: [...prevData.links, ...uniqueNewLinks]
        };
      });
      
    } catch (error) {
      console.error("Lỗi khi mở rộng:", error);
    }
  }, []);

  const handleExportPDF = async () =>{
      if(!searchResult) return;
      setIsExporting(true);
      try{
        const pdf = new jsPDF('p','mm', 'a4');
        pdf.setFont("helvetica", "bold");
        pdf.setFontSize(22);
        pdf.setTextColor(220, 38, 38); 
        pdf.text("BAO CAO PHAN TICH MA DOC", 105, 20, { align: "center" });
        pdf.setFont("helvetica", "normal");
        pdf.setFontSize(12);
        pdf.setTextColor(0, 0, 0);
        
        let currentY = 40;
        const lineSpacing = 8;
        pdf.text(`Ten ma doc: ${searchResult.iocValue}`, 20, currentY); currentY += lineSpacing;
        pdf.text(`Loai: ${searchResult.type}`, 20, currentY); currentY += lineSpacing;
        pdf.text(`Diem rui ro: ${searchResult.riskScore}/100`, 20, currentY); currentY += lineSpacing;
        pdf.text(`Quoc gia: ${searchResult.country}`, 20, currentY); currentY += lineSpacing;
        pdf.text(`Nguon tao: ${searchResult.asn}`, 20, currentY); currentY += lineSpacing;
        
        currentY += 10;
        pdf.setFont("helvetica", "bold");
        pdf.text("Danh sach cac IOC lien doi (Visual Graph):", 20, currentY);
  
        const graphElement = document.querySelector('.graph-panel');
        if (graphElement) {
          const canvas = await html2canvas(graphElement, { scale: 2, backgroundColor: '#0f172a', useCORS: true });
          const imgData = canvas.toDataURL('image/png');
          const imgWidth = 170; 
          const imgHeight = (canvas.height * imgWidth) / canvas.width;
          pdf.addImage(imgData, 'PNG', 20, currentY + 5, imgWidth, imgHeight);
        }
        pdf.save(`NexusTIP_Report_${searchResult.iocValue}.pdf`);
      } catch (err) {
        alert("Lỗi khi tạo file PDF!");
      } finally {
        setIsExporting(false);
      }
  };

  return (
    <div className="search-page-wrapper">
      <div className="search-header">
        <h1 className="search-title">Tra cứu Dấu vết Tấn công (IOC)</h1>
        <p className="search-subtitle">Hệ thống phân tích thông minh</p>
        <form className="search-input-group" onSubmit={handleSearch}>
          <input type="text" className="search-input" placeholder="Nhập IP, Domain, hoặc Hash..." value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
          <button type="submit" className="btn-search" disabled={isLoading}>{isLoading ? 'Đang quét...' : 'Truy vết 🔍'}</button>
        </form>
      </div>

      {searchResult && (
        <div className="result-container">
          <div className="info-panel">
            <h2 className="info-title">Chi tiết Tâm điểm</h2>
            <button onClick={handleExportPDF} disabled={isExporting} style={{ backgroundColor: '#10b981', color: '#fff', border: 'none', padding: '8px 15px', borderRadius: '6px', cursor: 'pointer', fontWeight: 'bold' }}>{isExporting ? 'Đang xuất...' : '📄 Xuất PDF'}</button>
            <div className="info-row"><span className="info-label">Giá trị:</span><strong className="info-value-large">{searchResult.iocValue}</strong></div>
            <div className="info-row"><span className="info-label">Loại:</span><span>{searchResult.type}</span></div>
            <div className="info-row"><span className="info-label">Điểm Rủi ro:</span><strong className="info-risk-high">{searchResult.riskScore} / 100</strong></div>
            <div className="info-row"><span className="info-label">Quốc gia & Nguồn:</span><span>{searchResult.country} - {searchResult.asn}</span></div>
          </div>

          <div className="graph-panel" style={{ overflow: 'hidden', position: 'relative' }}>
             
             {/* Cập nhật Legend (Chú giải) */}
             <div style={{ position: 'absolute', top: 15, left: 15, background: 'rgba(22, 27, 34, 0.8)', padding: '10px 15px', borderRadius: 8, border: '1px solid #30363d', fontSize: 13, zIndex: 10, textAlign: 'left' }}>
                <div style={{fontWeight: 'bold', marginBottom: 8, color: '#c9d1d9'}}>Mức độ rủi ro (Risk)</div>
                <div style={{color: '#a371f7', marginBottom: 4}}>🟣 Tâm điểm tra cứu</div>
                <div style={{color: '#58a6ff', marginBottom: 4}}>🔵 Domain (Tên miền)</div>
                <div style={{color: '#ff7b72', marginBottom: 4}}>🔴 Cao (&ge; 80)</div>
                <div style={{color: '#d29922', marginBottom: 4}}>🟡 Cảnh báo (&ge; 50)</div>
                <div style={{color: '#238636', marginBottom: 8}}>🟢 An toàn (&lt; 50)</div>
                <div style={{borderTop: '1px dashed #555', paddingTop: 8, color: '#10b981', fontWeight: 'bold'}}>🟢[+] Có thể mở rộng</div>
                <div style={{color: '#ffeb3b', fontStyle: 'italic', marginTop: 4}}>🖱️ Click chuột phải để mở</div>
             </div>

             {selectedNode && (
                <div style={{ position: 'absolute', top: 15, right: 15, background: 'rgba(15, 23, 42, 0.95)', padding: '15px 20px', borderRadius: '10px', border: '1px solid #38bdf8', fontSize: '14px', zIndex: 20, textAlign: 'left', minWidth: '220px', boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.5)' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #334155', paddingBottom: '8px', marginBottom: '12px' }}>
                        <strong style={{ color: '#38bdf8' }}>Chi tiết Node</strong>
                        <button onClick={() => setSelectedNode(null)} style={{ background: 'none', border: 'none', color: '#ef4444', cursor: 'pointer', fontWeight: 'bold' }}>X</button>
                    </div>
                    <div style={{ color: '#c9d1d9', marginBottom: '8px' }}><strong>Giá trị: </strong>{selectedNode.name}</div>
                    <div style={{ color: '#c9d1d9', marginBottom: '8px' }}><strong>Loại: </strong> <span style={{ color: selectedNode.color, fontWeight: 'bold' }}>{selectedNode.type}</span></div>
                    <div style={{ color: '#c9d1d9' }}><strong>Màu sắc nhận diện: </strong> <span style={{ display: 'inline-block', width: '12px', height: '12px', backgroundColor: selectedNode.color, borderRadius: '50%' }}></span></div>
                </div>
             )}

             {graphData.nodes.length > 0 ? (
               <ForceGraph2D
                  ref={fgRef}
                  graphData={graphData}
                  width={700}
                  height={500}
                  linkColor={() => 'rgba(255, 255, 255, 0.2)'}
                  linkWidth={1.5}
                  linkLabel=""
                  linkDirectionalArrowLength={4}
                  linkDirectionalArrowRelPos={1}
                  onNodeClick={(node) => setSelectedNode(node)} 
                  onNodeRightClick={handleNodeRightClick} 
                  onNodeHover={node => setHoverNode(node)}
                  nodeCanvasObject={(node, ctx, globalScale) => {
                    try {
                      const x = node.x || 0; 
                      const y = node.y || 0; 
                      const radius = node.val + 2;
                      
                      // 1. Vẽ Node (Hình tròn chính)
                      ctx.beginPath(); 
                      ctx.arc(x, y, radius, 0, 2 * Math.PI, false);
                      ctx.fillStyle = node.color; 
                      ctx.fill();
                      ctx.lineWidth = node.color === '#a371f7' ? 2 : 1; 
                      ctx.strokeStyle = '#ffffff'; 
                      ctx.stroke();

                      // 2. Viền Xanh khi User chọn Node
                      if (selectedNode && selectedNode.id === node.id) {
                          ctx.beginPath(); 
                          ctx.arc(x, y, radius + 3, 0, 2 * Math.PI, false);
                          ctx.strokeStyle = '#38bdf8'; 
                          ctx.lineWidth = 2; 
                          ctx.stroke();
                      }

                      // 3. VẼ HUY HIỆU DẤU CỘNG NẾU CÓ THỂ MỞ RỘNG (Visual Affordance)
                      if (node.isExpandable) {
                          const badgeRadius = 3.5; // Bán kính cục huy hiệu
                          // Đặt góc 45 độ (phía dưới bên phải của Node)
                          const badgeX = x + radius * Math.cos(Math.PI / 4);
                          const badgeY = y + radius * Math.sin(Math.PI / 4);

                          // Vẽ nền huy hiệu màu xanh
                          ctx.beginPath();
                          ctx.arc(badgeX, badgeY, badgeRadius, 0, 2 * Math.PI, false);
                          ctx.fillStyle = '#10b981'; // Xanh lá cây (Tailwind emerald-500)
                          ctx.fill();
                          ctx.strokeStyle = '#0f172a'; // Viền đen để tách biệt
                          ctx.lineWidth = 0.5;
                          ctx.stroke();

                          // Vẽ dấu cộng (+) màu trắng ở giữa
                          ctx.fillStyle = '#ffffff';
                          ctx.font = 'bold 7px Sans-Serif';
                          ctx.textAlign = 'center';
                          ctx.textBaseline = 'middle';
                          // Thêm 0.5 vào Y để chữ canh giữa hoàn hảo hơn
                          ctx.fillText('+', badgeX, badgeY + 0.5); 
                      }

                      // 4. Vẽ Text hiển thị khi Hover, Click hoặc Zoom to
                      const isHovered = hoverNode && hoverNode.id === node.id;
                      const isSelected = selectedNode && selectedNode.id === node.id;
                      const isZoomedIn = globalScale > 2.5;

                      if (isHovered || isSelected || isZoomedIn) {
                          const label = `[${node.type}] ${node.name}`;
                          const fontSize = 12 / globalScale; 
                          ctx.font = `${fontSize}px Sans-Serif`;
                          ctx.textAlign = 'center'; 
                          ctx.textBaseline = 'top'; 
                          
                          ctx.fillStyle = 'rgba(15, 23, 42, 0.85)';
                          const textWidth = ctx.measureText(label).width;
                          ctx.fillRect(x - textWidth/2 - 2, y + radius + 2, textWidth + 4, fontSize + 4);

                          ctx.fillStyle = isHovered ? '#38bdf8' : '#c9d1d9';
                          ctx.fillText(label, x, y + radius + 4);
                      }
                    } catch (err) {}
                  }}
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