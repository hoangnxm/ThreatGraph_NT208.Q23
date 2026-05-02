import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const IocManagement = () => {
    const [iocs, setIocs] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    
    // Lấy searchText từ thanh Search chung của hệ thống
    const context = useOutletContext();
    const searchText = context ? context[0] : '';

    // Phân trang & Lọc
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(10);
    const [totalCount, setTotalCount] = useState(0);
    const [typeFilter, setTypeFilter] = useState(''); // State mới cho Dropdown lọc Type

    // State quản lý Form
    const [showForm, setShowForm] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editingId, setEditingId] = useState(null);
    const [formData, setFormData] = useState({ type: 'IP', value: '', riskScore: 0, country: '', originRef: 'Manual Entry', tags: [] });
    const [showRelForm, setShowRelForm] = useState(false);
    const [relFormData, setRelFormData] = useState({ fromValue: '', toValue: '', relationType: 'related_to' });

    const fetchIocs = async () => {
        setLoading(true);
        try {
            const offset = (page - 1) * limit;
            // Nối động các tham số vào URL
            let url = `/iocnodes/paged?offset=${offset}&limit=${limit}`;
            if (typeFilter) url += `&type=${typeFilter}`;
            if (searchText) url += `&keyword=${searchText}`;

            const res = await axiosClient.get(url);
            setIocs(res.data.items || []);
            setTotalCount(res.data.totalCount || 0);
        } catch (err) {
            setError(err.response?.data?.message || err.message || "Lỗi tải dữ liệu.");
        } finally {
            setLoading(false);
        }
    };

    // Nếu gõ tìm kiếm hoặc đổi filter, phải reset về trang 1
    useEffect(() => {
        setPage(1);
    }, [searchText, typeFilter]);

    useEffect(() => { fetchIocs(); }, [page, limit, searchText, typeFilter]);

    // XÓA IOC
const handleDelete = async (id) => {
        if (!window.confirm("Bạn có chắc chắn muốn xóa IOC này không?")) return;
        try { await axiosClient.delete(`/iocnodes/${id}`); fetchIocs(); } 
        catch (err) { alert("Lỗi khi xóa: " + (err.response?.data?.message || err.message)); }
    };

    const handleEditClick = (ioc) => {
        setIsEditing(true); setEditingId(ioc.id);
        setFormData({ type: ioc.type, value: ioc.value, riskScore: ioc.riskScore, country: ioc.country || '', tags: ioc.tags || [] });
        setShowForm(true);
    };

    const handleSaveIoc = async (e) => {
        e.preventDefault();
        try {
            if (isEditing) {
                await axiosClient.put(`/iocnodes/${editingId}`, { 
                    riskScore: parseInt(formData.riskScore), 
                    country: formData.country, 
                    tags: formData.tags 
                });
            } else {
                await axiosClient.post('/iocnodes', { 
                    ...formData, 
                    riskScore: parseInt(formData.riskScore) 
                });
            }
            
            // Thành công thì đóng form và reset dữ liệu
            setShowForm(false);
            setFormData({ type: 'IP', value: '', riskScore: 0, country: '', tags: [] });
            fetchIocs();
            
        } catch (err) { 
            // KIỂM TRA NẾU LỖI LÀ 409 CONFLICT (Trùng lặp Node)
            if (err.response && err.response.status === 409) {
                const errData = err.response.data;
                
                let alertMsg = `⚠️ CẢNH BÁO TRÙNG LẶP:\n\n`;
                alertMsg += `${errData.message}\n`;
                alertMsg += `Nguồn gốc dữ liệu: ${errData.source}\n`;
                alertMsg += `ID của Node có sẵn: ${errData.existingKey}\n\n`;
                alertMsg += `Vui lòng sử dụng ID này để tạo liên kết thay vì thêm mới!`;
                
                alert(alertMsg);
            } else {
                // Các lỗi khác (500, 400...)
                alert("Lỗi khi lưu: " + (err.response?.data?.message || err.message)); 
            }
        }
    };

    const handleSaveRelationship = async (e) => {
        e.preventDefault();
        try {
            // Gọi API POST mà bạn vừa test ngon lành trên Swagger
            const res = await axiosClient.post('/iocnodes/relationship', relFormData);
            
            alert("✅ " + (res.data.message || 'Nối node thành công!'));
            
            // Đóng form và reset dữ liệu
            setShowRelForm(false);
            setRelFormData({ fromValue: '', toValue: '', relationType: 'related_to' });
        } catch (err) { 
            alert("❌ Lỗi khi nối node: " + (err.response?.data?.message || err.message)); 
        }
    };

    const getTypeStyle = (t) => {
        const type = String(t).toLowerCase();
        if (type === 'ip') return { bg: '#1e3a8a', text: '#93c5fd' };
        if (type === 'domain') return { bg: '#166534', text: '#86efac' };
        if (type === 'hash') return { bg: '#701a75', text: '#f0abfc' };
        return { bg: '#334155', text: '#cbd5e1' };
    };

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '20px', alignItems: 'center' }}>
                <h2 style={{ color: '#fff', margin: 0 }}>🛡️ QUẢN LÝ IOC</h2>
                
                {/* BỘ LỌC BỔ SUNG */}
                <div style={{ display: 'flex', gap: '10px' }}>
                    <select 
                        value={typeFilter} 
                        onChange={(e) => setTypeFilter(e.target.value)}
                        style={{ padding: '10px', borderRadius: '8px', backgroundColor: '#1e293b', color: '#fff', border: '1px solid #475569' }}
                    >
                        <option value="">Tất cả các loại</option>
                        <option value="IP">Chỉ xem IP</option>
                        <option value="Domain">Chỉ xem Domain</option>
                        <option value="Hash">Chỉ xem Hash</option>
                    </select>

                    <button 
                        onClick={() => { setShowRelForm(!showRelForm); setShowForm(false); }}
                        style={{ backgroundColor: '#8b5cf6', color: '#fff', padding: '10px 20px', borderRadius: '8px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                        {showRelForm ? 'Đóng Nối Node' : '🔗 Nối Node'}
                    </button>

                    <button 
                        onClick={() => { setIsEditing(false); setShowForm(!showForm); }}
                        style={{ backgroundColor: '#2563eb', color: '#fff', padding: '10px 20px', borderRadius: '8px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                        {showForm ? 'Đóng Form' : '+ Thêm IOC Mới'}
                    </button>
                </div>
            </div>

            {/* FORM NHẬP LIỆU (Tương tự UsersManagement.jsx) */}
            {showForm && (
                <form onSubmit={handleSaveIoc} style={{ backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', marginBottom: '20px', border: '1px solid #334155' }}>
                    <h3 style={{ color: '#93c5fd', marginTop: 0 }}>{isEditing ? `✏️ Sửa IOC: ${formData.value}` : '✨ Thêm IOC Mới'}</h3>
                    <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap' }}>
                        {!isEditing && (
                            <>
                                <select value={formData.type} onChange={e => setFormData({...formData, type: e.target.value})} style={inputStyle}>
                                    <option value="IP">IP</option>
                                    <option value="Domain">Domain</option>
                                    <option value="Hash">Hash</option>
                                </select>
                                <input placeholder="Giá trị (IP/Domain/Hash)" value={formData.value} onChange={e => setFormData({...formData, value: e.target.value})} style={inputStyle} required />
                            </>
                        )}
                        <input type="number" placeholder="Risk Score (0-100)" value={formData.riskScore} onChange={e => setFormData({...formData, riskScore: e.target.value})} style={inputStyle} />
                        <input placeholder="Quốc gia (VD: VN, US)" maxLength="2" value={formData.country} onChange={e => setFormData({...formData, country: e.target.value})} style={inputStyle} />
                        
                        <button type="submit" style={{ backgroundColor: '#16a34a', color: '#fff', padding: '10px 25px', borderRadius: '6px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                            {isEditing ? 'Lưu thay đổi' : 'Thêm vào hệ thống'}
                        </button>

                        <button 
                            type="button" 
                            onClick={() => {
                                setShowForm(false);
                                setIsEditing(false);
                                setEditingId(null);
                                setFormData({ type: 'IP', value: '', riskScore: 0, country: '', tags: [] });
                            }} 
                            style={{ backgroundColor: '#64748b', color: '#fff', padding: '10px 25px', borderRadius: '6px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                            Hủy
                        </button>
                    </div>
                </form>
            )}

            {/* FORM NHẬP LIỆU: TẠO LIÊN KẾT (RELATIONSHIP) */}
            {showRelForm && (
                <form onSubmit={handleSaveRelationship} style={{ backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', marginBottom: '20px', border: '1px dashed #8b5cf6' }}>
                    <h3 style={{ color: '#c4b5fd', marginTop: 0 }}>🔗 Tạo Liên Kết Giữa 2 Node</h3>
                    <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap', alignItems: 'center' }}>
                        
                        <input 
                            placeholder="Giá trị Node Nguồn (VD: 8.8.8.8)" 
                            value={relFormData.fromValue} 
                            onChange={e => setRelFormData({...relFormData, fromValue: e.target.value})} 
                            style={inputStyle} 
                            required 
                        />
                        <span style={{ color: '#94a3b8', fontWeight: 'bold' }}>🡲</span>
                        <input 
                            placeholder="Giá trị Node Đích (VD: google.com)" 
                            value={relFormData.toValue} 
                            onChange={e => setRelFormData({...relFormData, toValue: e.target.value})} 
                            style={inputStyle} 
                            required 
                        />
                        
                        <input 
                            placeholder="Loại quan hệ (VD: related_to)" 
                            value={relFormData.relationType} 
                            onChange={e => setRelFormData({...relFormData, relationType: e.target.value})} 
                            style={inputStyle} 
                            required 
                        />
                        
                        <button type="submit" style={{ backgroundColor: '#8b5cf6', color: '#fff', padding: '10px 25px', borderRadius: '6px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                            Xác nhận nối
                        </button>
                    </div>
                    <p style={{ color: '#64748b', fontSize: '0.85rem', marginTop: '10px', marginBottom: 0 }}>
                        * Gợi ý: Hãy copy cột ID (Key) của 2 IOC ở bảng bên dưới dán vào đây.
                    </p>
                </form>
            )}

            {/* BẢNG DỮ LIỆU */}
            <table style={{ width: '100%', color: '#e2e8f0', borderCollapse: 'collapse' }}>
                <thead>
                    <tr style={{ borderBottom: '2px solid #334155', textAlign: 'left' }}>
                        <th style={{ padding: '12px' }}>Giá trị</th>
                        <th>Loại</th>
                        <th>Độ rủi ro</th>
                        <th>Quốc gia</th>
                        <th>Nguồn tạo</th>
                        <th>Thao tác</th>
                    </tr>
                </thead>
                <tbody>
                    {/* KHÔNG dùng filter của Javascript nữa, lấy thẳng từ Backend */}
                    {iocs.map(ioc => (
                        <tr key={ioc.id} style={{ borderBottom: '1px solid #1e293b' }}>
                            <td style={{ padding: '15px 12px', fontFamily: 'monospace' }}>{ioc.value}</td>
                            <td><span style={{ backgroundColor: getTypeStyle(ioc.type).bg, color: getTypeStyle(ioc.type).text, padding: '4px 8px', borderRadius: '4px', fontWeight: 'bold', fontSize: '0.8rem' }}>{ioc.type}</span></td>
                            <td style={{ color: ioc.riskScore > 70 ? '#fca5a5' : '#86efac', fontWeight: 'bold' }}>{ioc.riskScore}</td>
                            <td>{ioc.country || '--'}</td>
                            <td style={{ color: '#94a3b8' }}>{ioc.originRef || 'AlienVault'}</td>
                            <td>
                                <button onClick={() => handleEditClick(ioc)} style={{ color: '#eab308', background: 'none', border: 'none', cursor: 'pointer', marginRight: '10px' }}>Sửa</button>
                                <button onClick={() => handleDelete(ioc.id)} style={{ color: '#ef4444', background: 'none', border: 'none', cursor: 'pointer' }}>Xóa</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {/* THANH ĐIỀU HƯỚNG PHÂN TRANG */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '20px', color: '#94a3b8' }}>
                <div>
                    Hiển thị {iocs.length} / Tổng số {totalCount} bản ghi
                </div>
                <div style={{ display: 'flex', gap: '10px' }}>
                    <button 
                        disabled={page === 1} 
                        onClick={() => setPage(page - 1)}
                        style={{ padding: '8px 16px', borderRadius: '6px', border: 'none', backgroundColor: page === 1 ? '#334155' : '#2563eb', color: '#fff', cursor: page === 1 ? 'not-allowed' : 'pointer' }}>
                        Quay lại
                    </button>
                    <span style={{ padding: '8px 16px', backgroundColor: '#1e293b', borderRadius: '6px', color: '#e2e8f0', fontWeight: 'bold' }}>
                        Trang {page} / {Math.ceil(totalCount / limit) || 1}
                    </span>
                    <button 
                        disabled={page >= Math.ceil(totalCount / limit)} 
                        onClick={() => setPage(page + 1)}
                        style={{ padding: '8px 16px', borderRadius: '6px', border: 'none', backgroundColor: page >= Math.ceil(totalCount / limit) ? '#334155' : '#2563eb', color: '#fff', cursor: page >= Math.ceil(totalCount / limit) ? 'not-allowed' : 'pointer' }}>
                        Tiếp theo
                    </button>
                </div>
            </div>
        </div>
    );
};
// Dán dòng này vào trước dòng export default
const inputStyle = { padding: '10px', borderRadius: '6px', backgroundColor: '#0f172a', color: '#fff', border: '1px solid #475569', minWidth: '150px' };

export default IocManagement;

