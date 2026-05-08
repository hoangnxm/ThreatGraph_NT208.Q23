import React, { useEffect, useState } from 'react';
import axiosClient from '../api/axiosClient';
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import { Doughnut } from 'react-chartjs-2';

// Đăng ký các thành phần của ChartJS[cite: 3]
ChartJS.register(ArcElement, Tooltip, Legend);

const Dashboard = () => {
    const [stats, setStats] = useState({ TotalUsers: 0, TotalLogs: 0 });
    const [chartData, setChartData] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchDashboardData = async () => {
            try {
                // 1. Lấy Stats 
                const resStats = await axiosClient.get('/Dashboard/stats');
                setStats(resStats.data);

                // 2. Lấy dữ liệu IOC
                const resIoc = await axiosClient.get('/iocnodes/paged?limit=10000');
                
                console.log("Raw Data từ API IOC:", resIoc.data); 

                const iocData = resIoc.data;
                const iocList = Array.isArray(iocData) ? iocData : 
                                (iocData?.Result || iocData?.result || iocData?.items || iocData?.data || []);

                console.log("Danh sách IOC sau khi lọc:", iocList);

                if (iocList.length > 0) {
                    const typeCounts = { IP: 0, Domain: 0, FileHash: 0, Other: 0 };
                    
                    iocList.forEach(item => {
                        const type = String(item.Type || item.type || "").toUpperCase();
                        if (type.includes('IP')) typeCounts.IP++;
                        else if (type.includes('DOMAIN')) typeCounts.Domain++;
                        else if (type.includes('HASH')) typeCounts.FileHash++;
                        else typeCounts.Other++;
                    });

                    setChartData({
                        labels: ['Địa chỉ IP', 'Tên miền (Domain)', 'Mã băm (FileHash)', 'Khác'],
                        datasets: [{
                            label: 'Số lượng dấu vết',
                            data: [typeCounts.IP, typeCounts.Domain, typeCounts.FileHash, typeCounts.Other],
                            backgroundColor: ['#3b82f6', '#a371f7', '#f59e0b', '#64748b'],
                            borderColor: '#0f172a',
                            borderWidth: 3,
                            hoverOffset: 10
                        }]
                    });
                } else {
                    // Nếu list rỗng, vẫn setChartData về 0 để nó không bị null gây lỗi render
                    setChartData({
                        labels: ['Chưa có data'],
                        datasets: [{ data: [0], backgroundColor: ['#64748b'] }]
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
            legend: {
                position: 'right',
                labels: { color: '#e2e8f0', font: { size: 14 } }
            }
        }
    };

    if (loading) return <div style={loadingTextStyle}>Đang nạp dữ liệu phân tích hệ thống...</div>;

    return (
        <div style={{ animation: 'fadeIn 0.5s' }}>
            <h2 style={{ color: '#f8fafc', marginBottom: '20px', fontWeight: 'bold' }}>
                📊 TỔNG QUAN HỆ THỐNG (IOC DASHBOARD)
            </h2>

            {/* Các thẻ con số tổng quan[cite: 3] */}
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
            
            {/* Biểu đồ Doughnut duy nhất tập trung vào IOC[cite: 1, 3] */}
            <div style={chartContainerStyle}>
                <h3 style={chartTitleStyle}>
                    Phân bổ các loại dấu vết mã độc (IOC Types)
                </h3>
                
                <div style={{ height: '350px', display: 'flex', justifyContent: 'center' }}>
                    {chartData && chartData.datasets[0].data.some(val => val > 0) ? (
                        <Doughnut data={chartData} options={chartOptions} />
                    ) : (
                        <div style={{ color: '#64748b', alignSelf: 'center' }}>
                            Chưa có dữ liệu IOC. Hãy thêm mới IP/Domain/Hash để xem phân tích.
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

// --- CSS Objects (Giữ nguyên phong cách UIT) ---
const loadingTextStyle = { color: '#38bdf8', padding: '20px', fontStyle: 'italic', textAlign: 'center' };
const cardStyle = {
    backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px',
    border: '1px solid #1e293b', borderLeft: '4px solid #3b82f6', 
    display: 'flex', alignItems: 'center', gap: '20px',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.5)'
};
const cardIconStyle = { fontSize: '2rem', padding: '15px', borderRadius: '12px' };
const cardLabelStyle = { color: '#94a3b8', fontSize: '0.85rem', fontWeight: 'bold', marginBottom: '5px' };
const cardValueStyle = { color: '#f8fafc', fontSize: '2rem', fontWeight: 'bold' };

const chartContainerStyle = { 
    backgroundColor: '#1e293b', 
    padding: '30px', 
    borderRadius: '16px', 
    marginTop: '30px',
    border: '1px solid #334155',
    boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.4)'
};
const chartTitleStyle = { 
    color: '#94a3b8', 
    marginBottom: '25px', 
    textAlign: 'center', 
    textTransform: 'uppercase', 
    fontSize: '0.9rem',
    letterSpacing: '1px'
};

export default Dashboard;