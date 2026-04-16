import React, { useState } from 'react';
import axiosClient from '../api/axiosClient';

const DataFeeds = () => {
    const [isSyncing, setIsSyncing] = useState(false);
    const [syncLog, setSyncLog] = useState([]);

    const handleSyncAlienVault = async () => {
        setIsSyncing(true);
        // Thêm log bắt đầu chạy
        const startTime = new Date().toLocaleTimeString();
        setSyncLog(prev => [{ time: startTime, msg: 'Đang kết nối tới máy chủ AlienVault OTX...', type: 'info' }, ...prev]);

        try {
            // Gọi API sang Backend
            const res = await axiosClient.post('/datafeeds/sync/alienvault');
            
            // Log thành công
            const endTime = new Date().toLocaleTimeString();
            setSyncLog(prev => [{ time: endTime, msg: res.data.message, type: 'success' }, ...prev]);
            
        } catch (err) {
            // Log thất bại
            const errorTime = new Date().toLocaleTimeString();
            const errorMsg = err.response?.data?.message || err.message || "Lỗi không xác định khi đồng bộ.";
            setSyncLog(prev => [{ time: errorTime, msg: `Lỗi: ${errorMsg}`, type: 'error' }, ...prev]);
        } finally {
            setIsSyncing(false);
        }
    };

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px', minHeight: '80vh' }}>
            <h2 style={{ color: '#fff', marginTop: 0, marginBottom: '20px' }}>📡 QUẢN LÝ DATA FEEDS</h2>

            {/* Panel AlienVault OTX */}
            <div style={{ backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', border: '1px solid #334155', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                    <h3 style={{ color: '#93c5fd', margin: '0 0 10px 0' }}>👽 AlienVault OTX</h3>
                    <p style={{ color: '#94a3b8', margin: 0, fontSize: '0.9rem' }}>
                        Tự động thu thập các Indicators of Compromise (IP, Domain, Hash) từ các chiến dịch (Pulses) mới nhất mà bạn đang theo dõi.
                    </p>
                </div>
                
                <button 
                    onClick={handleSyncAlienVault}
                    disabled={isSyncing}
                    style={{ 
                        backgroundColor: isSyncing ? '#475569' : '#2563eb', 
                        color: '#fff', 
                        padding: '12px 24px', 
                        borderRadius: '8px', 
                        border: 'none', 
                        cursor: isSyncing ? 'not-allowed' : 'pointer', 
                        fontWeight: 'bold',
                        minWidth: '180px'
                    }}>
                    {isSyncing ? '⏳ Đang đồng bộ...' : '🔄 Đồng bộ ngay'}
                </button>
            </div>

            {/* Bảng Nhật ký (Logs) */}
            <div style={{ marginTop: '30px', backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', border: '1px solid #334155' }}>
                <h4 style={{ color: '#e2e8f0', marginTop: 0, borderBottom: '1px solid #334155', paddingBottom: '10px' }}>📋 Nhật ký hoạt động</h4>
                
                {syncLog.length === 0 ? (
                    <p style={{ color: '#64748b', fontStyle: 'italic' }}>Chưa có hoạt động đồng bộ nào được ghi nhận.</p>
                ) : (
                    <ul style={{ listStyleType: 'none', padding: 0, margin: 0 }}>
                        {syncLog.map((log, index) => (
                            <li key={index} style={{ padding: '10px 0', borderBottom: '1px dashed #334155', color: log.type === 'error' ? '#fca5a5' : log.type === 'success' ? '#86efac' : '#cbd5e1' }}>
                                <span style={{ color: '#64748b', marginRight: '15px', fontFamily: 'monospace' }}>[{log.time}]</span>
                                {log.msg}
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
};

export default DataFeeds;