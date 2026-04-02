import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const AuditLogList = () => {
    const [logs, setLogs] = useState([]);
    const [loading, setLoading] = useState(true);
    
    const context = useOutletContext();
    const searchText = context ? context[0] : ''; 

    useEffect(() => {
        const fetchLogs = async () => {
            try {
                const res = await axiosClient.get('/Logs');
                setLogs(Array.isArray(res.data) ? res.data : (res.data?.Result || []));
            } catch (err) {
                console.error("Lỗi lấy nhật ký:", err);
            } finally {
                setLoading(false);
            }
        };
        fetchLogs();
    }, []);

    const filteredLogs = logs.filter(log => {
        const search = String(searchText || "").toLowerCase();
        const ip = String(log.ClientIp || log.clientIp || log.IP || "").toLowerCase();
        const action = String(log.Action || log.action || "").toLowerCase();
        
        // Lọc theo IP hoặc Hành động
        return ip.includes(search) || action.includes(search);
    });

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px', animation: 'fadeIn 0.5s' }}>
            <h2 style={{ color: '#fff', marginBottom: '20px' }}>📜 NHẬT KÝ HOẠT ĐỘNG HỆ THỐNG</h2>
            
            {loading ? (
                <div style={{ color: '#38bdf8', fontStyle: 'italic' }}>Đang quét lịch sử...</div>
            ) : (
                <table style={{ width: '100%', color: '#e2e8f0', textAlign: 'left', borderCollapse: 'collapse' }}>
                    <thead>
                        <tr style={{ borderBottom: '2px solid #334155', backgroundColor: '#1e293b' }}>
                            <th style={{ padding: '12px' }}>Thời gian</th>
                            <th>Người dùng</th>
                            <th>Hành động</th>
                            <th>IP Client</th>
                        </tr>
                    </thead>
                    <tbody>
                        {filteredLogs.map((log, index) => (
                            <tr key={log._key || index} style={{ borderBottom: '1px solid #1e293b', ':hover': { backgroundColor: '#1e293b' } }}>
                                <td style={{ padding: '12px' }}>{new Date(log.Timestamp || log.timestamp).toLocaleString('vi-VN')}</td>
                                <td style={{ fontWeight: 'bold', color: '#93c5fd' }}>{log.Username || log.username || 'System'}</td>
                                <td>
                                    <span style={{ 
                                        backgroundColor: String(log.Action || '').includes('DELETE') ? '#7f1d1d' : '#1e3a8a',
                                        padding: '4px 8px', borderRadius: '4px', fontSize: '0.8rem'
                                    }}>
                                        {log.Action || log.action}
                                    </span>
                                </td>
                                <td style={{ fontFamily: 'monospace', color: '#fbbf24' }}>{log.ClientIp || log.clientIp || log.IP || 'N/A'}</td>
                            </tr>
                        ))}
                        {filteredLogs.length === 0 && (
                            <tr><td colSpan="4" style={{ textAlign: 'center', padding: '20px' }}>Không tìm thấy dấu vết nào khớp với '{searchText}'</td></tr>
                        )}
                    </tbody>
                </table>
            )}
        </div>
    );
};

export default AuditLogList;