import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode'; 
import MainLayout from './layouts/MainLayout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import UsersManagement from './pages/UsersManagement';
import AuditLogList from './pages/AuditLogList';
import SearchPage from './pages/SearchPage/SearchPage';
import IocManagement from './pages/IocManagement';
import DataFeeds from './pages/DataFeeds';
import { useEffect } from 'react';

function App() {
  const token = localStorage.getItem('token');
  useEffect(() => {
    const syncLogout = (event) => {
      if (event.key === 'token' && event.newValue === null) {
        window.location.href = '/login'; 
      }
    };

    window.addEventListener('storage', syncLogout);
    return () => window.removeEventListener('storage', syncLogout);
  }, []);
  const getRoleFromToken = () => {
    if (!token) return null;
    try {
      const decoded = jwtDecode(token);
      return decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;
    } catch (error) {
      return null;
    }
  };

  const role = getRoleFromToken();

  return (
    <HashRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        
        {/* Chỉ cần có Token là được vào khung giao diện chính */}
        <Route path="/" element={token ? <MainLayout /> : <Navigate to="/login" />}>
          <Route index element={<Dashboard />} />
          <Route path='search' element={<SearchPage/>}/>
          <Route path="database" element={<IocManagement/>} />

          {/* Kiểm tra Role từ Token: Nếu là Admin mới cho vào, không thì đá về Dashboard */}
          <Route 
            path="users" 
            element={role === 'Admin' ? <UsersManagement /> : <Navigate to='/'/>} 
          />

          <Route 
            path="logs" 
            element={role === 'Admin' ? <AuditLogList /> : <Navigate to='/'/>} 
          />

          <Route 
            path="feeds" 
            element={role === 'Admin' ? <DataFeeds /> : <Navigate to='/'/>} 
          />
        </Route>
      </Routes>
    </HashRouter>
  );
}
export default App;