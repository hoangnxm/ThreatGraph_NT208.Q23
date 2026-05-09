import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const navigate = useNavigate();

    const handleLogin = async (e) => {
        e.preventDefault();
        setError('');

        // VALIDATION MẬT KHẨU
        const trimmedPassword = password.trim();

        // 1. Kiểm tra độ dài: Tối thiểu 6, tối đa 15 ký tự
        if (trimmedPassword.length < 6 || trimmedPassword.length > 15) {
            setError('Mật khẩu phải từ 6 đến 15 ký tự. Không được nhập quá ngắn!');
            return;
        }

        // 2. Kiểm tra độ phức tạp: Bắt buộc có chữ cái thường, chữ cái hoa và chữ số
        const strongRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;
        if (!strongRegex.test(trimmedPassword)) {
            setError('Mật khẩu yếu! Yêu cầu chứa ít nhất 1 chữ cái thường, 1 chữ cái hoa và 1 chữ số.');
            return;
        }
        setIsLoading(true);

        try {
            // Gọi API Login
            const response = await axiosClient.post('/Auth/login', {
                username: username,
                password: trimmedPassword
            });

            if (response.data && response.data.token) {
                localStorage.clear();
                localStorage.setItem('token', response.data.token);

                window.location.href = '/';
            }
        } catch (err) {
            if (err.response && err.response.data) {
                setError(err.response.data.message || 'Lỗi xác thực từ Server!');
            } else {
                setError('Không thể kết nối đến máy chủ Backend!');
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div style={containerStyle}>
            <div style={cardStyle}>
                <div style={{ textAlign: 'center', marginBottom: '30px' }}>
                    <h1 style={{ color: '#fff', fontSize: '1.8rem', marginBottom: '10px' }}>🛡️ HỆ THỐNG IOC</h1>
                    <p style={{ color: '#94a3b8', fontSize: '0.9rem' }}>Đăng nhập kênh quản trị bảo mật</p>
                </div>

                {/* Hiện thông báo lỗi nếu có */}
                {error && (
                    <div style={errorStyle}>
                        ⚠️ {error}
                    </div>
                )}

                <form onSubmit={handleLogin} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                    <div>
                        <label style={labelStyle}>Tài khoản</label>
                        <input 
                            type="text" 
                            placeholder="Username"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            style={inputStyle}
                            required
                        />
                    </div>

                    <div>
                        <label style={labelStyle}>Mật khẩu</label>
                        <input 
                            type="password" 
                            placeholder="Password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            style={inputStyle}
                            required
                        />
                    </div>

                    <button 
                        type="submit" 
                        disabled={isLoading}
                        style={isLoading ? btnDisabledStyle : btnStyle}
                    >
                        {isLoading ? 'ĐANG XÁC THỰC...' : 'ĐĂNG NHẬP HỆ THỐNG'}
                    </button>
                </form>
            </div>
        </div>
    );
};

// --- CSS HỆ DARK MODE ---
const containerStyle = {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100vh',
    backgroundColor: '#020617',
    fontFamily: 'sans-serif'
};

const cardStyle = {
    backgroundColor: '#0f172a',
    padding: '40px',
    borderRadius: '16px',
    boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.5), 0 0 15px 0 rgba(59, 130, 246, 0.2)', // Có tí glow xanh
    width: '100%',
    maxWidth: '400px',
    border: '1px solid #1e293b'
};

const labelStyle = {
    display: 'block',
    color: '#cbd5e1',
    marginBottom: '8px',
    fontSize: '0.9rem',
    fontWeight: '500'
};

const inputStyle = {
    width: '100%',
    padding: '12px 15px',
    backgroundColor: '#1e293b',
    border: '1px solid #334155',
    borderRadius: '8px',
    color: '#fff',
    outline: 'none',
    boxSizing: 'border-box',
    fontSize: '1rem',
    transition: 'border-color 0.2s'
};

const errorStyle = {
    backgroundColor: '#7f1d1d',
    color: '#fecaca',
    padding: '10px 15px',
    borderRadius: '8px',
    marginBottom: '20px',
    fontSize: '0.85rem',
    border: '1px solid #991b1b'
};

const btnStyle = {
    width: '100%',
    padding: '14px',
    backgroundColor: '#2563eb', 
    color: 'white',
    border: 'none',
    borderRadius: '8px',
    fontSize: '1rem',
    fontWeight: 'bold',
    cursor: 'pointer',
    marginTop: '10px',
    transition: 'background-color 0.2s'
};

const btnDisabledStyle = {
    ...btnStyle,
    backgroundColor: '#475569',
    color: '#94a3b8',
    cursor: 'not-allowed'
};

export default Login;