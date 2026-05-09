import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import { Pie } from 'react-chartjs-2';
import { jwtDecode } from 'jwt-decode';

ChartJS.register(ArcElement, Tooltip, Legend);

const Dashboard = () => {
    const [stats, setStats] = useState({ TotalUsers: 0, TotalLogs: 0, TotalIocs: 0, IocsToday: 0, TotalEdges: 0, TopIps: [] });
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
                setStats(res.data);

                // Giả lập dữ liệu Pie Chart từ IocNodes thực tế
                const iocRes = await axiosClient.get('/iocnodes/paged?limit=1000');
                const iocList = iocRes.data?.items || [];
                const counts = { IP: 0, Domain: 0, Hash: 0 };
                iocList.forEach(i => {
                    if (i.Type === 'IP') counts.IP++;
                    else if (i.Type === 'Domain') counts.Domain++;
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
            <h2>📊 HỆ THỐNG GIÁM SÁT IOC</h2>

            {/* CARD CHO ADMIN */}
            {userRole === 'Admin' && (
                <div style={{ display: 'flex', gap: '20px', marginBottom: '20px' }}>
                    <div style={adminCardStyle}>👥 Users: {stats.TotalUsers}</div>
                    <div style={adminCardStyle}>📜 Logs: {stats.TotalLogs}</div>
                </div>
            )}

            {/* 3 CARD CHO MỌI USER */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '20px' }}>
                <div style={userCardStyle}>🦠 Tổng IOC: {stats.TotalIocs}</div>
                <div style={userCardStyle}>🔥 Thêm hôm nay: {stats.IocsToday}</div>
                <div style={userCardStyle}>🕸️ Edges: {stats.TotalEdges}</div>
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
                            {stats.TopIps?.map((ip, i) => (
                                <tr key={i} onClick={() => navigate(`/search?query=${ip.Value}`)} style={{ cursor: 'pointer' }}>
                                    <td style={{ color: '#ef4444' }}>{ip.Value}</td>
                                    <td>{ip.Source}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
};

// Style tạm thời
const adminCardStyle = { background: '#1e293b', padding: '20px', borderRadius: '10px', flex: 1, borderLeft: '5px solid #3b82f6' };
const userCardStyle = { background: '#0f172a', padding: '30px', borderRadius: '12px', fontSize: '1.2rem', fontWeight: 'bold', border: '1px solid #334155' };
const chartBoxStyle = { background: '#0f172a', padding: '20px', borderRadius: '15px' };

export default Dashboard;