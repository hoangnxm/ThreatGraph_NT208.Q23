import React from 'react';
import { useNavigate } from 'react-router-dom';

const Header = ({ onSearch }) => {
    const navigate = useNavigate();

    const handleLogout = () => {
        localStorage.removeItem('token');
        navigate('/login');
    };

    return (
        <header style={{ height: '70px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0 30px', backgroundColor: '#0f172a', borderBottom: '1px solid #1e293b' }}>
            <input 
                type="text" 
                placeholder="🔍 Tìm kiếm nhanh hệ thống..." 
                onChange={(e) => onSearch(e.target.value)}
                style={{ padding: '10px 15px', borderRadius: '8px', border: '1px solid #334155', width: '350px', backgroundColor: '#1e293b', color: '#fff' }}
            />
            <button onClick={handleLogout} style={{ padding: '8px 16px', backgroundColor: '#ef4444', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer' }}>Đăng xuất</button>
        </header>
    );
};
export default Header;