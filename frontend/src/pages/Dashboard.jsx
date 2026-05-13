import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import { Pie } from 'react-chartjs-2';
import { jwtDecode } from 'jwt-decode';

ChartJS.register(ArcElement, Tooltip, Legend);

const Dashboard = () => {
    // Đã sửa lại state mặc định an toàn tuyệt đối
    const [stats, setStats] = useState({ 
        totalUsers: 0, totalLogs: 0, totalIocs: 0, iocsToday: 0, totalEdges: 0, topIps: [] 
    });
    const [chartData, setChartData] = useState(null);
    const [userRole, setUserRole] = useState('');
    const navigate = useNavigate();

    useEffect(() => {
        const token = localStorage.getItem('token');
        if (token) {
            const decoded = jwtDecode(token);
            setUserRole(decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role);
        }

        const fetchData = async () => {
            try {
                const res = await axiosClient.get('/Dashboard/stats');
                
                // Đã sửa lại phần gán dữ liệu để tránh lỗi do API trả về sai định dạng
                setStats({
                    totalUsers: res.data.totalUsers ?? res.data.TotalUsers ?? 0,
                    totalLogs: res.data.totalLogs ?? res.data.TotalLogs ?? 0,
                    totalIocs: res.data.totalIocs ?? res.data.TotalIocs ?? 0,
                    iocsToday: res.data.iocsToday ?? res.data.IocsToday ?? 0,
                    totalEdges: res.data.totalEdges ?? res.data.TotalEdges ?? 0,
                    topIps: res.data.topIps ?? res.data.TopIps ?? []
                });

                const iocRes = await axiosClient.get('/iocnodes/paged?limit=1000');
                const iocList = iocRes.data?.items || [];
                const counts = { IP: 0, Domain: 0, Hash: 0 };
                
                iocList.forEach(i => {
                    const type = i.type || i.Type;
                    if (type === 'IP') counts.IP++;
                    else if (type === 'Domain') counts.Domain++;
                    else counts.Hash++;
                });

                setChartData({
                    labels: ['IP', 'Domain', 'Hash'],
                    datasets: [{
                        data: [counts.IP, counts.Domain, counts.Hash],
                        backgroundColor: ['#ef4444', '#3b82f6', '#f59e0b'],
                        hoverOffset: 20
                    }]
                });
            } catch (err) { console.error(err); }
        };
        fetchData();
    }, []);

    return (
        <div style={{ color: '#fff', padding: '20px' }}>
            <h2>HỆ THỐNG GIÁM SÁT IOC</h2>
           {userRole === 'Admin' && (
                <div style={{ display: 'flex', gap: '20px', marginBottom: '20px' }}>
                    <div style={adminCardStyle}>Users: {stats.totalUsers}</div>
                    <div style={adminCardStyle}>Logs: {stats.totalLogs}</div>
                </div>
            )}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '20px' }}>
                <div style={userCardStyle}>Tổng IOC: {stats.totalIocs}</div>
                <div style={userCardStyle}>Thêm hôm nay: {stats.iocsToday}</div>
                <div style={userCardStyle}>Edges: {stats.totalEdges}</div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '30px', marginTop: '30px' }}>
                <div style={chartBoxStyle}>
                    <h3>Tỉ lệ mã độc</h3>
                    <div style={{ height: '250px' }}>
                        {chartData && <Pie data={chartData} options={{ maintainAspectRatio: false }} />}
                    </div>
                </div>

                <div style={chartBoxStyle}>
                    <h3>Top 10 IP Nguy Hiểm</h3>
                    <table style={{ width: '100%', textAlign: 'left' }}>
                        <thead><tr><th>IP</th><th>Nguồn</th></tr></thead>
                        <tbody>
                            {stats.topIps && stats.topIps.length > 0 ? (
                                stats.topIps.map((ip, i) => (                             
                                    <tr key={i} onClick={() => navigate(`/search?query=${ip.value || ip.Value}`)} style={{ cursor: 'pointer' }}>
                                        <td style={{ color: '#ef4444' }}>{ip.value || ip.Value}</td>
                                        <td>{ip.originRef || ip.OriginRef || 'Unknown'}</td>
                                    </tr>
                                ))
                            ) : (
                                <tr>
                                    <td colSpan="2" style={{ textAlign: 'center', color: '#94a3b8' }}>
                                        Chưa có dữ liệu IP nguy hiểm
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
};

const adminCardStyle = { background: '#1e293b', padding: '20px', borderRadius: '10px', flex: 1, borderLeft: '5px solid #3b82f6' };
const userCardStyle = { background: '#0f172a', padding: '30px', borderRadius: '12px', fontSize: '1.2rem', fontWeight: 'bold', border: '1px solid #334155' };
const chartBoxStyle = { background: '#0f172a', padding: '20px', borderRadius: '15px' };

export default Dashboard;
