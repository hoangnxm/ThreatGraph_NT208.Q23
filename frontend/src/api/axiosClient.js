import axios from 'axios';

const axiosClient = axios.create({
    baseURL: import.meta.env.VITE_API_URL || "http://20.196.128.188:7193/api",
    // baseURL: import.meta.env.VITE_API_URL || "https://localhost:7193/api",
    headers: { 'Content-Type': 'application/json' }
});

// Tự động gắn Token vào mọi request
axiosClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

// Xử lý lỗi 401 và 403 toàn cục
axiosClient.interceptors.response.use(
    (response) => {
        return response;
    },
    (error) => {
        // Trích xuất thêm config để biết đường link API nào vừa gọi bị lỗi
        const originalRequest = error.config;

        if (error.response) {
            // LỖI 401: Token hết hạn hoặc không hợp lệ, chỉ redirect nếu không phải đang gọi API login
            if (error.response.status === 401 && originalRequest.url !== '/Auth/login') {
                localStorage.removeItem('token');
                window.location.href = '/login';
            }
            // LỖI 403: Người dùng không có quyền truy cập tài nguyên
            else if (error.response.status === 403) {
                alert("Bạn không có quyền thực hiện chức năng hoặc xem nội dung này!");
            }
        }
        return Promise.reject(error);
    }
);
export default axiosClient;