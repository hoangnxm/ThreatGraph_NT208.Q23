import React, { useEffect, useState } from 'react';
import axiosClient from '../api/axiosClient';
// Import thư viện vẽ biểu đồ
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import { Doughnut } from 'react-chartjs-2';

// Đăng ký các thành phần của ChartJS
ChartJS.register(ArcElement, Tooltip, Legend);

const Dashboard = () => {
    const [stats, setStats] = useState({ TotalUsers: 0, TotalLogs: 0 });
    const [chartData, setChartData] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchDashboardData = async () => {
            try {
                // 1. Gọi API lấy con số tổng quan (Cái này ông đã có)
                const resStats = await axiosClient.get('/Dashboard/stats');
                setStats(resStats.data);

                // 2. Gọi API Logs để vẽ biểu đồ phân tích hành vi
                const resLogs = await axiosClient.get('/Logs');
                const logs = Array.isArray(resLogs.data) ? resLogs.data : (resLogs.data?.Result || []);

                // Đếm phân loại hành động của người dùng
                const actionCounts = { GET: 0, POST: 0, PUT: 0, DELETE: 0, OTHER: 0 };
                
                logs.forEach(log => {
                    const act = String(log.Action || log.action || "").toUpperCase();
                    if (act.includes('GET')) actionCounts.GET++;
                    else if (act.includes('POST')) actionCounts.POST++;
                    else if (act.includes('PUT') || act.includes('PATCH')) actionCounts.PUT++;
                    else if (act.includes('DELETE')) actionCounts.DELETE++;
                    else actionCounts.OTHER++;
                });

                setChartData({
                    labels: ['Truy xuất (GET)', 'Thêm mới (POST)', 'Cập nhật (PUT)', 'Xóa (DELETE)', 'Khác'],
                    datasets: [
                        {
                            label: 'Số lượng sự kiện',
                            data: [actionCounts.GET, actionCounts.POST, actionCounts.PUT, actionCounts.DELETE, actionCounts.OTHER],
                            backgroundColor: [
                                '#3b82f6', // Xanh dương cho GET
                                '#22c55e', // Xanh lá cho POST
                                '#eab308', // Vàng cho PUT
                                '#ef4444', // Đỏ cho DELETE
                                '#64748b'  // Xám cho Khác
                            ],
                            borderColor: '#0f172a', // Viền tệp màu nền website
                            borderWidth: 3,
                            hoverOffset: 4
                        },
                    ],
                });

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
            legend: {
                position: 'right',
                labels: { color: '#e2e8f0', font: { size: 14 } }
            }
        }
    };

    if (loading) return <div style={{ color: '#38bdf8', padding: '20px', fontStyle: 'italic' }}>Đang nạp dữ liệu phân tích hệ thống...</div>;

    return (
        <div style={{ animation: 'fadeIn 0.5s' }}>
            <h2 style={{ color: '#f8fafc', marginBottom: '20px' }}>📊 TỔNG QUAN HỆ THỐNG (IOC DASHBOARD)</h2>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '20px' }}>
                <div style={cardStyle}>
                    <div style={{ ...cardIconStyle, color: '#60a5fa', backgroundColor: '#1e3a8a' }}>👥</div>
                    <div>
                        <div style={cardLabelStyle}>TỔNG TÀI KHOẢN</div>
                        <div style={cardValueStyle}>{stats.TotalUsers || stats.totalUsers || 0}</div>
                    </div>
                </div>

                <div style={{ ...cardStyle, borderLeft: '4px solid #ef4444' }}>
                    <div style={{ ...cardIconStyle, color: '#ef4444', backgroundColor: '#7f1d1d' }}>📜</div>
                    <div>
                        <div style={cardLabelStyle}>SỰ KIỆN BẢO MẬT (LOGS)</div>
                        <div style={cardValueStyle}>{stats.TotalLogs || stats.totalLogs || 0}</div>
                    </div>
                </div>
            </div>
            
            <div style={{ 
                backgroundColor: '#1e293b', 
                padding: '25px', 
                borderRadius: '12px', 
                marginTop: '30px',
                border: '1px solid #334155',
                boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.5)'
            }}>
                <h3 style={{ color: '#94a3b8', marginBottom: '20px', textAlign: 'center', textTransform: 'uppercase', fontSize: '1rem' }}>
                    Phân bổ lưu lượng hành động API
                </h3>
                
                <div style={{ height: '300px', display: 'flex', justifyContent: 'center' }}>
                    {chartData && chartData.datasets[0].data.some(val => val > 0) ? (
                        <Doughnut data={chartData} options={chartOptions} />
                    ) : (
                        <div style={{ color: '#64748b', alignSelf: 'center' }}>Chưa có dữ liệu Log để vẽ biểu đồ. Hãy thử thêm/xóa user để tạo Log.</div>
                    )}
                </div>
            </div>
        </div>
    );
};

// --- CSS Objects ---
const cardStyle = {
    backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px',
    border: '1px solid #1e293b', borderLeft: '4px solid #3b82f6', 
    display: 'flex', alignItems: 'center', gap: '20px',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.5)'
};
const cardIconStyle = { fontSize: '2rem', padding: '15px', borderRadius: '12px' };
const cardLabelStyle = { color: '#94a3b8', fontSize: '0.85rem', fontWeight: 'bold', marginBottom: '5px' };
const cardValueStyle = { color: '#f8fafc', fontSize: '2rem', fontWeight: 'bold' };

export default Dashboard;