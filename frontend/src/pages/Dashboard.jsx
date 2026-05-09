import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import { Pie } from 'react-chartjs-2'; 
import { jwtDecode } from 'jwt-decode';

// Đăng ký các thành phần của ChartJS
ChartJS.register(ArcElement, Tooltip, Legend);

const Dashboard = () => {
    const [stats, setStats] = useState({ TotalUsers: 0, TotalLogs: 0, TotalIocs: 0, IocsToday: 0, TotalEdges: 0, TopIps: [] });
    const [chartData, setChartData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [userRole, setUserRole] = useState('');
    const navigate = useNavigate();

    useEffect(() => {
        // Lấy Role từ token
        const token = localStorage.getItem('token');
        if (token) {
            try {
                const decoded = jwtDecode(token);
                const role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;
                setUserRole(role);
            } catch (error) {
                console.error("Lỗi giải mã token", error);
            }
        }

        const fetchDashboardData = async () => {
            try {
                // Lấy dữ liệu Stats tổng hợp
                const resStats = await axiosClient.get('/Dashboard/stats');
                setStats(resStats.data || {});

                // Lấy dữ liệu IOC để vẽ Pie Chart
                const resIoc = await axiosClient.get('/iocnodes/paged?limit=10000');
                const iocData = resIoc.data;
                const iocList = Array.isArray(iocData) ? iocData : (iocData?.Result || iocData?.result || iocData?.items || iocData?.data || []);

                if (iocList.length > 0) {
                    const typeCounts = { IP: 0, Domain: 0, FileHash: 0, Other: 0 };
                    
                    iocList.forEach(item => {
                        const type = String(item.Type || item.type || "").toUpperCase();
                        if (type.includes('IP')) typeCounts.IP++;
                        else if (type.includes('DOMAIN')) typeCounts.Domain++;
                        else if (type.includes('HASH')) typeCounts.FileHash++;
                        else typeCounts.Other++;
                    });

                    // Tính % để hiển thị trong Tooltip (ChartJS sẽ tự động tính tổng dựa trên data)
                    const total = iocList.length;

                    setChartData({
                        labels: ['Địa chỉ IP', 'Tên miền (Domain)', 'Mã băm (FileHash)', 'Khác'],
                        datasets: [{
                            label: 'Tỉ lệ phần trăm',
                            data: [typeCounts.IP, typeCounts.Domain, typeCounts.FileHash, typeCounts.Other],
                            backgroundColor: ['#ef4444', '#3b82f6', '#f59e0b', '#64748b'],
                            borderColor: '#0f172a',
                            borderWidth: 2,
                            hoverOffset: 15 
                        }]
                    });
                } else {
                    setChartData({
                        labels: ['Chưa có data'],
                        datasets: [{ data: [1], backgroundColor: ['#64748b'] }]
                    });
                }

            } catch (err) {
                console.error("Lỗi lấy dữ liệu Dashboard:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchDashboardData();
    }, []);

    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { position: 'right', labels: { color: '#e2e8f0', font: { size: 14 } } },
            tooltip: {
                callbacks: {
                    label: function(context) {
                        let label = context.label || '';
                        let value = context.raw || 0;
                        let total = context.chart._metasets[context.datasetIndex].total;
                        let percentage = ((value / total) * 100).toFixed(2) + '%';
                        return `${label}: ${value} (${percentage})`;
                    }
                }
            }
        }
    };

    if (loading) return <div style={loadingTextStyle}>Đang nạp dữ liệu phân tích hệ thống...</div>;

    return (
        <div style={{ animation: 'fadeIn 0.5s' }}>
            <h2 style={{ color: '#f8fafc', marginBottom: '20px', fontWeight: 'bold' }}>
                📊 TỔNG QUAN HỆ THỐNG IOC ({userRole})
            </h2>

            {/* HIỂN THỊ RIÊNG CHO ADMIN: Thêm thông tin User & Log */}
            {userRole === 'Admin' && (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px', marginBottom: '20px' }}>
                    <div style={cardStyle}>
                        <div style={{ ...cardIconStyle, color: '#60a5fa', backgroundColor: '#1e3a8a' }}>👥</div>
                        <div>
                            <div style={cardLabelStyle}>TỔNG TÀI KHOẢN</div>
                            <div style={cardValueStyle}>{stats.TotalUsers}</div>
                        </div>
                    </div>
                    <div style={{ ...cardStyle, borderLeft: '4px solid #ef4444' }}>
                        <div style={{ ...cardIconStyle, color: '#ef4444', backgroundColor: '#7f1d1d' }}>📜</div>
                        <div>
                            <div style={cardLabelStyle}>SỰ KIỆN LOGS</div>
                            <div style={cardValueStyle}>{stats.TotalLogs}</div>
                        </div>
                    </div>
                </div>
            )}

            {/* 3 THẺ TỔNG QUAN CHUNG CHO MỌI ROLE */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '20px' }}>
                <div style={{...cardStyle, borderLeft: '4px solid #10b981'}}>
                    <div style={{ ...cardIconStyle, color: '#10b981', backgroundColor: '#064e3b' }}>🦠</div>
                    <div>
                        <div style={cardLabelStyle}>TỔNG SỐ IOC</div>
                        <div style={cardValueStyle}>{stats.TotalIocs}</div>
                    </div>
                </div>

                <div style={{...cardStyle, borderLeft: '4px solid #f59e0b'}}>
                    <div style={{ ...cardIconStyle, color: '#f59e0b', backgroundColor: '#78350f' }}>🔥</div>
                    <div>
                        <div style={cardLabelStyle}>IOC THÊM HÔM NAY</div>
                        <div style={cardValueStyle}>{stats.IocsToday}</div>
                    </div>
                </div>

                <div style={{...cardStyle, borderLeft: '4px solid #8b5cf6'}}>
                    <div style={{ ...cardIconStyle, color: '#8b5cf6', backgroundColor: '#4c1d95' }}>🕸️</div>
                    <div>
                        <div style={cardLabelStyle}>SỐ LIÊN KẾT (EDGES)</div>
                        <div style={cardValueStyle}>{stats.TotalEdges}</div>
                    </div>
                </div>
            </div>
            
            {/* VÙNG CHỨA BIỂU ĐỒ VÀ TOP 10 */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginTop: '30px' }}>
                
                {/* Biểu đồ Tròn (Pie Chart) */}
                <div style={containerBoxStyle}>
                    <h3 style={chartTitleStyle}>Phân bổ loại mã độc</h3>
                    <div style={{ height: '300px', display: 'flex', justifyContent: 'center' }}>
                        {chartData ? <Pie data={chartData} options={chartOptions} /> : <div>Không có dữ liệu</div>}
                    </div>
                </div>

                {/* Bảng Top 10 IP Nguy Hiểm */}
                <div style={containerBoxStyle}>
                    <h3 style={chartTitleStyle}>Top 10 IP Nguy Hiểm Nhất Tuần</h3>
                    <div style={{ overflowY: 'auto', maxHeight: '300px' }}>
                        <table style={{ width: '100%', borderCollapse: 'collapse', color: '#f8fafc' }}>
                            <thead>
                                <tr style={{ borderBottom: '1px solid #334155', textAlign: 'left', color: '#94a3b8' }}>
                                    <th style={{ padding: '10px' }}>#</th>
                                    <th style={{ padding: '10px' }}>Địa chỉ IP</th>
                                    <th style={{ padding: '10px' }}>Nguồn (Source)</th>
                                </tr>
                            </thead>
                            <tbody>
                                {(stats.TopIps || []).map((ipObj, index) => (
                                    <tr 
                                        key={index} 
                                        onClick={() => navigate(`/search?query=${ipObj.Value || ipObj.value}`)}
                                        style={{ borderBottom: '1px solid #1e293b', cursor: 'pointer', transition: 'background 0.2s' }}
                                        onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#1e293b'}
                                        onMouseOut={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
                                    >
                                        <td style={{ padding: '10px' }}>{index + 1}</td>
                                        <td style={{ padding: '10px', color: '#ef4444', fontWeight: 'bold' }}>{ipObj.Value || ipObj.value}</td>
                                        <td style={{ padding: '10px' }}>{ipObj.Source || ipObj.source || 'AlienVault'}</td>
                                    </tr>
                                ))}
                                {(!stats.TopIps || stats.TopIps.length === 0) && (
                                    <tr><td colSpan="3" style={{ padding: '20px', textAlign: 'center', color: '#64748b' }}>Chưa có dữ liệu IP</td></tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    );
};

// --- CSS Objects ---
const loadingTextStyle = { color: '#38bdf8', padding: '20px', fontStyle: 'italic', textAlign: 'center' };
const cardStyle = {
    backgroundColor: '#0f172a', padding: '20px', borderRadius: '12px',
    border: '1px solid #1e293b', display: 'flex', alignItems: 'center', gap: '15px',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.5)'
};
const cardIconStyle = { fontSize: '1.8rem', padding: '12px', borderRadius: '10px' };
const cardLabelStyle = { color: '#94a3b8', fontSize: '0.8rem', fontWeight: 'bold', marginBottom: '5px' };
const cardValueStyle = { color: '#f8fafc', fontSize: '1.8rem', fontWeight: 'bold' };

const containerBoxStyle = { 
    backgroundColor: '#0f172a', padding: '25px', borderRadius: '16px', 
    border: '1px solid #1e293b', boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.5)'
};
const chartTitleStyle = { 
    color: '#94a3b8', marginBottom: '20px', textTransform: 'uppercase', fontSize: '0.9rem', letterSpacing: '1px'
};

export default Dashboard;