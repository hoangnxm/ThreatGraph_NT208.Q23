import React, { useEffect, useState } from 'react';
import axiosClient from '../api/axiosClient';

const Dashboard = () => {
    const [stats, setStats] = useState({ TotalUsers: 0, TotalLogs: 0 });
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchStats = async () => {
            try {
                const res = await axiosClient.get('/Dashboard/stats');
                setStats(res.data);
            } catch (err) {
                console.error("Lỗi lấy thống kê:", err);
            } finally {
                setLoading(false);
            }
        };
        fetchStats();
    }, []);

    if (loading) return <div style={{ color: '#94a3b8' }}>Đang quét hệ thống...</div>;

    return (
        <div style={{ animation: 'fadeIn 0.5s' }}>
            <h2 style={{ color: '#f8fafc', marginBottom: '20px' }}>📊 TỔNG QUAN HỆ THỐNG (IOC DASHBOARD)</h2>
            
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '20px' }}>
                
                {/* Card 1: Tổng User */}
                <div style={cardStyle}>
                    <div style={cardIconStyle}>👥</div>
                    <div>
                        <div style={cardLabelStyle}>TỔNG TÀI KHOẢN</div>
                        <div style={cardValueStyle}>{stats.TotalUsers}</div>
                    </div>
                </div>

                {/* Card 2: Tổng Logs */}
                <div style={{ ...cardStyle, borderLeft: '4px solid #ef4444' }}>
                    <div style={{ ...cardIconStyle, color: '#ef4444', backgroundColor: '#7f1d1d' }}>📜</div>
                    <div>
                        <div style={cardLabelStyle}>SỰ KIỆN BẢO MẬT (LOGS)</div>
                        <div style={cardValueStyle}>{stats.TotalLogs}</div>
                    </div>
                </div>

            </div>
            
            {/* Vùng trống để tuần sau vẽ biểu đồ */}
            <div style={{ ...cardStyle, marginTop: '20px', height: '300px', display: 'flex', justifyContent: 'center', alignItems: 'center', borderLeft: 'none' }}>
                <span style={{ color: '#475569' }}>[Khu vực tích hợp Biểu đồ Chart.js (Đang phát triển)]</span>
            </div>
        </div>
    );
};

// --- CSS Objects ---
const cardStyle = {
    backgroundColor: '#0f172a',
    padding: '25px',
    borderRadius: '12px',
    border: '1px solid #1e293b',
    borderLeft: '4px solid #3b82f6',
    display: 'flex',
    alignItems: 'center',
    gap: '20px',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.5)'
};
const cardIconStyle = { fontSize: '2rem', backgroundColor: '#1e3a8a', color: '#60a5fa', padding: '15px', borderRadius: '12px' };
const cardLabelStyle = { color: '#94a3b8', fontSize: '0.85rem', fontWeight: 'bold', marginBottom: '5px' };
const cardValueStyle = { color: '#f8fafc', fontSize: '2rem', fontWeight: 'bold' };

export default Dashboard;