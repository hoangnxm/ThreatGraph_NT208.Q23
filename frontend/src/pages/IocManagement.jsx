
import React, { useEffect, useState, useCallback } from 'react';
import { useOutletContext } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { jwtDecode } from 'jwt-decode';

const IocManagement = () => {
    // Khởi tạo mảng chứa danh sách các IOC sẽ hiển thị lên bảng dữ liệu
    const [iocs, setIocs] = useState([]);

    // Trạng thái dùng để hiển thị biểu tượng "Đang tải" khi chờ API phản hồi
    const [loading, setLoading] = useState(true);

    // Biến lưu trữ nội dung thông báo lỗi nếu việc gọi API thất bại
    const [error, setError] = useState('');

    // Lấy dữ liệu chia sẻ từ Layout chung
    const context = useOutletContext();

    // Trích xuất từ khóa tìm kiếm từ context, nếu không có thì để rỗng
    const searchText = context ? context[0] : '';

    // Lưu trữ số trang hiện tại mà người dùng đang xem
    const [page, setPage] = useState(1);

    // Quy định số lượng bản ghi tối đa hiển thị trên mỗi trang 
    const [limit, setLimit] = useState(10);

    // Lưu tổng số lượng IOC hiện có trong Database để tính toán phân trang
    const [totalCount, setTotalCount] = useState(0);

    // Lưu trữ tiêu chí lọc IP, Domain hoặc Hash từ người dùng
    const [typeFilter, setTypeFilter] = useState('');

    // Quản lý giá trị số trang trong ô nhập liệu thủ công của người dùng
    const [inputPage, setInputPage] = useState(1);

    // Tự động tính toán tổng số trang dựa trên tổng bản ghi chia cho giới hạn mỗi trang
    const totalPages = Math.ceil(totalCount / limit) || 1;

    // Hiển thị số trang thực tế khi trang thay đổi
    useEffect(() => {
        setInputPage(page);
    }, [page]);

    const handleInputPageChange = (e) => {
        // Chỉ cho phép nhập số nguyên
        const val = e.target.value.replace(/[^0-9]/g, '');
        setInputPage(val);
    };

    const handleInputPageSubmit = (e) => {
        // Chỉ chạy khi bấm Enter hoặc click chuột ra ngoài
        if (e.key === 'Enter' || e.type === 'blur') {
            let value = parseInt(inputPage, 10);

            if (isNaN(value) || value < 1) {
                value = 1;
            } else if (value > totalPages) {
                value = totalPages;
            }
            setPage(value);
            setInputPage(value);
        }
    };

    // Điều khiển việc Ẩn hoặc Hiện cái bảng nhập liệu thêm/sửa IOC
    const [showForm, setShowForm] = useState(false);

    // Dùng để thay đổi tiêu đề cái bảng và đổi nút "Lưu" thành "Cập nhật"
    const [isEditing, setIsEditing] = useState(false);

    // Lưu lại cái ID của IOC đang chọn để sửa
    const [editingId, setEditingId] = useState(null);

    // Thùng chứa dữ liệu thực tế của form IOC (Loại, Giá trị, Điểm rủi ro...)
    const [formData, setFormData] = useState({
        type: 'IP',
        value: '',
        riskScore: 0,
        country: '',
        originRef: 'Manual Entry',
        tags: []
    });

    // Điều khiển việc Ẩn hoặc Hiện cái bảng tạo Liên kết giữa 2 IOC
    const [showRelForm, setShowRelForm] = useState(false);

    // Thùng chứa dữ liệu để tạo liên kết: "Nguồn" nối với "Đích" bằng "Quan hệ" gì
    const [relFormData, setRelFormData] = useState({
        fromValue: '',
        toValue: '',
        relationType: 'related_to'
    });

    // useCallback để lưu hàm vào bộ nhớ tránh khởi tạo hàm vô ích
    const fetchIocs = useCallback(async () => {
        // Bật trạng thái đang chạy
        setLoading(true);
        try {
            // Tính vị trí dòng đầu tiên của các trang
            const offset = (page - 1) * limit;

            // Tạo URL với tham số offset và limit
            let url = `/iocnodes/paged?offset=${offset}&limit=${limit}`;

            // Nếu lọc nối thêm vào URL
            if (typeFilter) url += `&type=${typeFilter}`;

            // Nếu tìm kiếm nối thêm vào URL
            if (searchText) url += `&keyword=${searchText}`;

            // Gọi API
            const res = await axiosClient.get(url);
            // Cập nhật dữ liệu IOC
            setIocs(res.data.items || []);
            // Cập nhật tổng IOC
            setTotalCount(res.data.totalCount || 0);
        } catch (err) {
            setError(err.response?.data?.message || err.message || "Lỗi tải dữ liệu.");
        } finally {
            setLoading(false);
        }
    }, [page, limit, typeFilter, searchText]);

    // Nếu gõ tìm kiếm hoặc đổi filter, phải reset về trang 1
    useEffect(() => {
        setPage(1);
    }, [searchText, typeFilter]);

    // Gọi API
    useEffect(() => {
        fetchIocs();
    }, [fetchIocs]);

    // Xóa IOC
    const handleDelete = async (id) => {
        // Hiển thị bảng thông báo xác nhận của trình duyệt để tránh bấm nhầm
        if (!window.confirm("Bạn có chắc chắn muốn xóa IOC này không?")) return;

        try {
            // Gọi lệnh DELETE tới API Backend kèm theo ID của IOC cần xóa
            await axiosClient.delete(`/iocnodes/${id}`);

            // Xóa thành công thì gọi lại hàm fetchIocs để cập nhật lại cái bảng
            fetchIocs();
        }
        catch (err) {
            alert("Lỗi khi xóa: " + (err.response?.data?.message || err.message));
        }
    };

    // HÀM CHECK QUYỀN CHUẨN JWT CỦA ASP.NET CORE
    const getUserRole = () => {
        const token = localStorage.getItem('token');
        if (!token) return '';
        try {
            const decoded = jwtDecode(token);
            // Hứng chuẩn cái Claim Role mà C# trả về
            return decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role || '';
        } catch (e) {
            return '';
        }
    };

    const userRole = getUserRole();

    const handleDeleteAll = async () => {
        const confirmDelete = window.confirm(
            "CẢNH BÁO: Hành động này sẽ xóa TOÀN BỘ IOC và các quan hệ trong hệ thống. Bạn có chắc chắn không?"
        );

        if (confirmDelete) {
            try {
                await axiosClient.delete('/IocNodes/all');
                alert("Đã dọn dẹp sạch hệ thống!");
                fetchIocs();
            } catch (error) {
                alert("Lỗi: " + (error.response?.data?.message || "Không thể thực hiện lệnh xóa"));
            }
        }
    };

    const handleEditClick = (ioc) => {
        // Chuyển trạng thái cờ hiệu sang "Đang sửa" để đổi tiêu đề Form thành "Cập nhật IOC"
        setIsEditing(true);

        // Ghi nhớ lại ID của cái IOC đang chọn để tí nữa bấm Lưu còn biết đường mà cập nhật đúng thằng đó
        setEditingId(ioc.id);

        // Đổ toàn bộ dữ liệu hiện có của hàng đó vào các ô nhập liệu (Form)
        setFormData({
            type: ioc.type,
            value: ioc.value,
            riskScore: ioc.riskScore,
            country: ioc.country || '',
            tags: ioc.tags || []
        });

        // Cuối cùng mới bật cái Popup (Modal) chứa Form lên cho người dùng thấy
        setShowForm(true);
    };

    // Validate dữ liệu trước khi gửi
    const validateFormData = () => {
        const { type, value, riskScore, country } = formData;

        // Chỉ validate Type và Value khi THÊM MỚI
        if (!isEditing) {
            if (type === 'IP') {
                const ipRegex = /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;
                if (!ipRegex.test(value)) {
                    alert("❌ Lỗi: Địa chỉ IP không hợp lệ! (VD: 192.168.1.1)");
                    return false;
                }
            } else if (type === 'Domain') {
                const domainRegex = /^(?!:\/\/)([a-zA-Z0-9-_]+\.)+[a-zA-Z]{2,11}?$/;
                if (!domainRegex.test(value)) {
                    alert("❌ Lỗi: Domain không hợp lệ! (VD: google.com, không bao gồm http://)");
                    return false;
                }
            } else if (type === 'Hash') {
                const hashRegex = /^([a-fA-F0-9]{32}|[a-fA-F0-9]{40}|[a-fA-F0-9]{64})$/;
                if (!hashRegex.test(value)) {
                    alert("❌ Lỗi: Hash không hợp lệ! Phải là định dạng MD5, SHA-1, hoặc SHA-256.");
                    return false;
                }
            }
        }

        // Validate những trường dùng chung cho cả THÊM và SỬA

        // Kiểm tra Risk Score (0 - 100)
        const score = parseInt(riskScore, 10);
        if (isNaN(score) || score < 0 || score > 100) {
            alert("❌ Lỗi: Risk Score phải là một số nguyên nằm trong khoảng từ 0 đến 100!");
            return false;
        }

        // Kiểm tra Quốc gia (Country)
        if (country && !/^[a-zA-Z]{2}$/.test(country)) {
            alert("❌ Lỗi: Mã Quốc gia chỉ được chứa đúng 2 chữ cái (VD: VN, US)!");
            return false;
        }

        return true; // Dữ liệu hợp lệ
    };

    // Lưu IOC
    const handleSaveIoc = async (e) => {
        e.preventDefault();

        if (!validateFormData()) return;

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
            setFormData({ type: 'IP', value: '', riskScore: 0, country: '', originRef: 'Manual Entry', tags: [] });
            fetchIocs();

        } catch (err) {
            if (err.response && err.response.status === 409) {
                const errData = err.response.data;

                let alertMsg = `⚠️ CẢNH BÁO TRÙNG LẶP:\n\n`;
                alertMsg += `${errData.message}\n`;
                alertMsg += `Nguồn gốc dữ liệu: ${errData.source}\n`;
                alertMsg += `ID của Node có sẵn: ${errData.existingKey}\n\n`;
                alertMsg += `Vui lòng sử dụng ID này để tạo liên kết thay vì thêm mới!`;

                alert(alertMsg);
            } else {
                alert("Lỗi khi lưu: " + (err.response?.data?.message || err.message));
            }
        }
    };

    // Lưu mối liên kết
    const handleSaveRelationship = async (e) => {
        e.preventDefault();
        try {
            const res = await axiosClient.post('/iocnodes/relationship', relFormData);
            alert("✅ " + (res.data.message || 'Nối node thành công!'));
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
                <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                    <h2 style={{ color: '#fff', margin: 0 }}>🛡️ QUẢN LÝ IOC</h2>

                    {/* NÚT XÓA TẤT CẢ NẰM Ở ĐÂY - CHỈ HIỆN KHI LÀ ADMIN */}
                    {userRole === 'Admin' && (
                        <button
                            onClick={handleDeleteAll}
                            style={{ backgroundColor: '#dc2626', color: '#fff', padding: '8px 16px', borderRadius: '8px', border: 'none', cursor: 'pointer', fontWeight: 'bold', boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)' }}
                            title="Xóa toàn bộ IOC và các mối quan hệ"
                        >
                            🗑️ Xóa Tất Cả
                        </button>
                    )}
                </div>

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

            {showForm && (
                <form onSubmit={handleSaveIoc} style={{ backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', marginBottom: '20px', border: '1px solid #334155' }}>
                    <h3 style={{ color: '#93c5fd', marginTop: 0 }}>{isEditing ? `✏️ Sửa IOC: ${formData.value}` : '✨ Thêm IOC Mới'}</h3>
                    <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap' }}>
                        {!isEditing && (
                            <>
                                <select value={formData.type} onChange={e => setFormData({ ...formData, type: e.target.value })} style={inputStyle}>
                                    <option value="IP">IP</option>
                                    <option value="Domain">Domain</option>
                                    <option value="Hash">Hash</option>
                                </select>
                                <input placeholder="Giá trị (IP/Domain/Hash)" value={formData.value} onChange={e => setFormData({ ...formData, value: e.target.value })} style={inputStyle} required />
                            </>
                        )}
                        <input type="number" placeholder="Risk Score (0-100)" min="0" max="100" value={formData.riskScore} onChange={e => setFormData({ ...formData, riskScore: e.target.value })} style={inputStyle} />
                        <input placeholder="Quốc gia (VD: VN, US)" maxLength="2" value={formData.country} onChange={e => setFormData({ ...formData, country: e.target.value })} style={inputStyle} />

                        <button type="submit" style={{ backgroundColor: '#16a34a', color: '#fff', padding: '10px 25px', borderRadius: '6px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                            {isEditing ? 'Lưu thay đổi' : 'Thêm vào hệ thống'}
                        </button>

                        <button
                            type="button"
                            onClick={() => {
                                setShowForm(false);
                                setIsEditing(false);
                                setEditingId(null);
                                setFormData({ type: 'IP', value: '', riskScore: 0, country: '', originRef: 'Manual Entry', tags: [] });
                            }}
                            style={{ backgroundColor: '#64748b', color: '#fff', padding: '10px 25px', borderRadius: '6px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                            Hủy
                        </button>
                    </div>
                </form>
            )}

            {showRelForm && (
                <form onSubmit={handleSaveRelationship} style={{ backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', marginBottom: '20px', border: '1px dashed #8b5cf6' }}>
                    <h3 style={{ color: '#c4b5fd', marginTop: 0 }}>🔗 Tạo Liên Kết Giữa 2 Node</h3>
                    <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap', alignItems: 'center' }}>

                        <input
                            placeholder="Giá trị Node Nguồn (VD: 8.8.8.8)"
                            value={relFormData.fromValue}
                            onChange={e => setRelFormData({ ...relFormData, fromValue: e.target.value })}
                            style={inputStyle}
                            required
                        />
                        <span style={{ color: '#94a3b8', fontWeight: 'bold' }}>🡲</span>
                        <input
                            placeholder="Giá trị Node Đích (VD: google.com)"
                            value={relFormData.toValue}
                            onChange={e => setRelFormData({ ...relFormData, toValue: e.target.value })}
                            style={inputStyle}
                            required
                        />

                        <input
                            placeholder="Loại quan hệ (VD: related_to)"
                            value={relFormData.relationType}
                            onChange={e => setRelFormData({ ...relFormData, relationType: e.target.value })}
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
                    <span style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '4px 16px', backgroundColor: '#1e293b', borderRadius: '6px', color: '#e2e8f0', fontWeight: 'bold' }}>
                        Trang
                        <input
                            type="text"
                            value={inputPage}
                            onChange={handleInputPageChange}
                            onKeyDown={handleInputPageSubmit}
                            onBlur={handleInputPageSubmit}
                            style={{ width: '45px', padding: '4px', textAlign: 'center', borderRadius: '6px', border: '1px solid #475569', backgroundColor: '#0f172a', color: '#fff', fontWeight: 'bold' }}
                        />
                        / {totalPages}
                    </span>
                    <button
                        disabled={page >= totalPages}
                        onClick={() => setPage(page + 1)}
                        style={{ padding: '8px 16px', borderRadius: '6px', border: 'none', backgroundColor: page >= totalPages ? '#334155' : '#2563eb', color: '#fff', cursor: page >= totalPages ? 'not-allowed' : 'pointer' }}>
                        Tiếp theo
                    </button>
                </div>
            </div>
        </div>
    );
};

const inputStyle = { padding: '10px', borderRadius: '6px', backgroundColor: '#0f172a', color: '#fff', border: '1px solid #475569', minWidth: '150px' };

export default IocManagement;