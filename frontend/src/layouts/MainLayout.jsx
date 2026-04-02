import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import Header from '../components/Header';

const MainLayout = () => {
    const [searchText, setSearchText] = useState('');

    return (
        <div style={{ 
            display: 'flex', 
            height: '100vh', 
            width: '100%', 
            backgroundColor: '#020617', 
            color: '#f8fafc', 
            overflow: 'hidden' 
        }}>
            <Sidebar />
            <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
                <Header onSearch={setSearchText} />
                
                <main style={{ 
                    padding: '25px', 
                    flex: 1, 
                    overflowY: 'auto', 
                    backgroundColor: '#020617',
                    width: '100%',          
                    maxWidth: '100%',        
                    boxSizing: 'border-box', 
                    display: 'flex',
                    flexDirection: 'column'
                }}>
                    <Outlet context={[searchText]} />
                </main>
            </div>
        </div>
    );
};

export default MainLayout;