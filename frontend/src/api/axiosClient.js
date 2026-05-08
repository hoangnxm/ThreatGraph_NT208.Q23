import axios from 'axios';

const axiosClient = axios.create({
    baseURL: 'http://localhost:5113/api',
    headers: { 'Content-Type': 'application/json' }
});

// Tự động gắn Token vào mọi request
axiosClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

// Xử lý lỗi 401 và 403
axiosClient.interceptors.response.use(
    (response) => {
        return response;
    },
    (error) => {
        if (error.response) {
            // LỖI 401: Token hết hạn hoặc không hợp lệ -> Xóa token và redirect về login
            if (error.response.status === 401) {
                localStorage.removeItem('token');
                window.location.href = '/login';
            }
            // LỖI 403: Không có quyền -> Hiển thị cảnh báo nhưng KHÔNG xóa token, KHÔNG redirect
            else if (error.response.status === 403) {
                alert("Bạn không có quyền thực hiện chức năng hoặc xem nội dung này!");
            }
        }
        return Promise.reject(error);
    }
);

export default axiosClient;