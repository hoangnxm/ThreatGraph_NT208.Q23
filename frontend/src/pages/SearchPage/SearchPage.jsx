import React, {useState} from 'react';
import './SearchPage.css';

function SearchPage(){

  // Lưu chữ user đang nhập
  const [searchInput, setSearchInput] = useState('');
  // Kết quả sau khi user bấm tìm kiếm
  const [searchResult, setSearchResult] = useState(false);
  // Trạng thái nút bấm
  const [isLoading, setIsLoading] = useState(false);

  // Xử lí sự kiện click nút tìm
  const handleSearch = (event) => {
    event.preventDefault(); // Tránh reload trang khi user bấm tìm

    if(searchInput.trim() === '') {return;}
    setIsLoading(true);
    
    // Dữ liệu giả
    setTimeout(() => {
      const mockData = {
        iocValue: searchInput,
        type: "IPv4 Address",
        riskScore: 92,
        country: "Nga 🇷🇺",
        asn: "AS12345 (HackerNetwork)",
        tags: ["Malware C2", "Botnet", "Tor Exit Node"],
      };
      
      setSearchResult(mockData);
      setIsLoading(false);
    }, 1000);
  };


  // Giao diện
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
            </div>
          </div>

        </div>
      )}
    </div>
  );
}

export default SearchPage;