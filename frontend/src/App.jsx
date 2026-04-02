import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import MainLayout from './layouts/MainLayout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import UsersManagement from './pages/UsersManagement';
import AuditLogList from './pages/AuditLogList';
import SearchPage from './pages/SearchPage/SearchPage';

function App() {
  const token = localStorage.getItem('token');
  const role = localStorage.getItem('role');

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        
        {/* Nhóm các trang cần đăng nhập vào MainLayout */}
        <Route path="/" element={token ? <MainLayout /> : <Navigate to="/login" />}>
          <Route index element={<Dashboard />} />
          <Route path='search' element={<SearchPage/>}/>
          <Route path="database" element={<Dashboard />} />

          {/* Các chức năng dành cho Admin */}
          <Route 
          path="users" 
          element={role === 'Admin' ? <UsersManagement /> :  <Navigate to='/'/>} 
          />

          <Route 
          path="logs" 
          element={role === 'Admin' ? <AuditLogList /> :  <Navigate to='/'/>} 
          />

          <Route 
          path="feeds" 
          element={role === 'Admin' ? <Dashboard /> :  <Navigate to='/'/>} 
          />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
export default App;