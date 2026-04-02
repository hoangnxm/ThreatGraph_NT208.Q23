import axios from 'axios';

const axiosClient = axios.create({
    baseURL: `${import.meta.env.VITE_API_URL}/api` || 'https://localhost:7193/api',
    headers: { 'Content-Type': 'application/json' }
});

// Tự động gắn Token vào mọi request
axiosClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

export default axiosClient;