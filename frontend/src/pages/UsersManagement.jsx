import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const UsersManagement = () => {
    const [users, setUsers] = useState([]);
    const [searchText] = useOutletContext();
    const [loading, setLoading] = useState(true); 
    const [error, setError] = useState('');
    const getRoleBadge = (role) => {
    const r = String(role || "").toLowerCase();
    if (r === 'admin') return { bg: '#7f1d1d', text: '#fca5a5' }; // Admin: Nền đỏ đô, chữ hồng
    if (r === 'user') return { bg: '#1e3a8a', text: '#93c5fd' };  // User: Nền xanh đen, chữ xanh lơ
    return { bg: '#334155', text: '#cbd5e1' };                    // Khác: Màu xám
}
    const fetchUsers = async () => {
        setLoading(true);
        setError('');
        try {
            const res = await axiosClient.get('/Users');
            console.log("CỤC DATA TỪ BACKEND TRẢ VỀ:", res.data);
            setUsers(Array.isArray(res.data) ? res.data : res.data.data || []);
        } catch (err) {
            console.error("Lỗi lấy danh sách user:", err);
            setError(err.response?.data?.message || err.message || "Lỗi không xác định từ Backend!");
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (key) => {
        if (window.confirm("Bạn có chắc muốn xóa tài khoản này?")) {
            try {
                await axiosClient.delete(`/Users/${key}`);
                fetchUsers();
            } catch (err) {
                alert("Lỗi khi xóa: " + (err.response?.data?.message || err.message));
            }
        }
    };

    useEffect(() => { fetchUsers(); }, []);

   const filteredUsers = Array.isArray(users) ? users.filter(u => {
        const name = String(u.username || u.Username || "");
        const search = String(searchText || "");
        
        return name.toLowerCase().includes(search.toLowerCase());
    }) : [];

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px' }}>
            <h2 style={{ color: '#fff', marginBottom: '20px' }}>QUẢN LÝ TÀI KHOẢN</h2>
            
            {/* Hiển thị lỗi nếu có */}
            {error && <div style={{ backgroundColor: '#7f1d1d', color: '#fecaca', padding: '10px', borderRadius: '8px', marginBottom: '15px' }}>⚠️ Lỗi: {error}</div>}
            {loading ? (
                <div style={{ color: '#38bdf8', fontStyle: 'italic' }}>Đang quét dữ liệu từ ArangoDB...</div>
            ) : (
                <table style={{ width: '100%', color: '#e2e8f0', textAlign: 'left', borderCollapse: 'collapse' }}>
                    <thead>
                        <tr style={{ borderBottom: '2px solid #334155' }}>
                            <th style={{ padding: '12px' }}>Username</th>
                            <th>Quyền hạn</th>
                            <th>Trạng thái</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
               {filteredUsers.map(u => (
                <tr key={u._key || Math.random()} style={{ borderBottom: '1px solid #1e293b' }}>
                <td style={{ padding: '15px 12px', fontWeight: 'bold' }}>
                    {u.username || u.Username || "Lỗi tàng hình"}
                    </td>
                    <td>
    <span style={{ 
        backgroundColor: getRoleBadge(u.role || u.Role).bg, 
        color: getRoleBadge(u.role || u.Role).text, 
        padding: '4px 8px', borderRadius: '4px', fontSize: '0.8rem', fontWeight: 'bold' 
    }}>
        {u.role || u.Role || "Unknown"}
    </span>
</td>
                             <td>
                                {(u.isLocked === true || u.IsLocked === true) ? '🔴 Bị khóa' : '🟢 Hoạt động'}
                                </td>
                                <td>
            <button onClick={() => handleDelete(u._key || u.Key)} style={{ color: '#ef4444', background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                Xóa
            </button>
        </td>
        
    </tr>
))}
                    </tbody>
                </table>
            )}
        </div>
    );
};
export default UsersManagement;