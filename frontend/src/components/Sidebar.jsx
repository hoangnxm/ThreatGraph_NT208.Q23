import React from 'react';
import { Link } from 'react-router-dom';

const Sidebar = () => {
    const linkStyle = { color: '#94a3b8', textDecoration: 'none', padding: '15px 20px', display: 'block', borderBottom: '1px solid #1e293b' };
    return (
        <div style={{ width: '250px', backgroundColor: '#0f172a', borderRight: '1px solid #1e293b' }}>
            <h2 style={{ color: '#fff', textAlign: 'center', padding: '20px 0', borderBottom: '1px solid #1e293b' }}>🛡️ IOC SYSTEM</h2>
            <nav>
                <Link to="/" style={linkStyle}>📊 Dashboard</Link>
                <Link to="/users" style={linkStyle}>👥 Quản lý User</Link>
                <Link to="/logs" style={linkStyle}>📜 Nhật ký hệ thống</Link>
            </nav>
        </div>
    );
};
export default Sidebar;