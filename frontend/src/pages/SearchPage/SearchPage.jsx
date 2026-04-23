import React, { useState } from 'react';
import ForceGraph2D from 'react-force-graph-2d'; // Đã thêm thư viện vẽ Graph
import './SearchPage.css';

function SearchPage(){

  const [searchInput, setSearchInput] = useState('');
  const [searchResult, setSearchResult] = useState(false);
  const [graphData, setGraphData] = useState(null); // Thêm state lưu dữ liệu Graph
  const [isLoading, setIsLoading] = useState(false);

  const handleSearch = async (event) => {
    event.preventDefault(); 

    if(searchInput.trim() === '') {return;}
    setIsLoading(true);
    setSearchResult(null);
    setGraphData(null); // Reset Graph khi quét mã mới
    
    try{
      // 1. Quét dữ liệu chi tiết IOC
      const response = await fetch(`https://localhost:7193/api/Search/${searchInput}`);

      if(!response.ok){
        if(response.status === 404){
          alert("Không tìm thấy dấu vết mã độc này trong hệ thống!");
        }
        else{
          alert("Lỗi Server!");
        }
        setIsLoading(false);
        return;
      }

      const data = await response.json();

      const realData = {
        iocValue: data.value,
        type: data.type,
        riskScore: data.riskScore,
        country: data.country,
        asn: data.originRef,
        tags: data.tags
      };

      setSearchResult(realData);

      // 2. Tự động gọi API quét dữ liệu Mạng nhện (Graph)
      try {
        const graphResponse = await fetch(`https://localhost:7193/api/Graph/${data.id}`);
        if (graphResponse.ok) {
          const gData = await graphResponse.json();
          setGraphData(gData);
        }
      } catch (graphError) {
        console.error("Lỗi khi kéo dữ liệu Graph:", graphError);
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

          <div className="graph-panel">
            <div className="graph-content">
              <h3 className="graph-title">🕸️ Khu vực Mạng nhện (Graph)</h3>
              
              {/* Khu vực render Component biểu đồ */}
              <div style={{ height: '400px', width: '100%', display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                {graphData && graphData.nodes && graphData.nodes.length > 0 ? (
                  <ForceGraph2D
                    graphData={graphData}
                    nodeLabel={(node) => `${node.name} (${node.type})`} // Tên hiển thị khi trỏ chuột
                    nodeColor={(node) => node.color} 
                    nodeVal={(node) => node.val}     
                    linkColor={() => '#94a3b8'}      // Màu liên kết
                    width={550}                      // Chỉnh lại width cho vừa flex box
                    height={400}
                    linkDirectionalArrowLength={3.5} // Mũi tên chỉ hướng quan hệ
                    linkDirectionalArrowRelPos={1}
                  />
                ) : (
                  <p style={{ color: '#64748b', fontSize: '0.9rem' }}>Chưa có dữ liệu liên kết cho IOC này.</p>
                )}
              </div>

            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default SearchPage;