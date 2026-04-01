import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import Header from '../components/Header';

const MainLayout = () => {
    const [searchText, setSearchText] = useState('');

    return (
        <div style={{ display: 'flex', height: '100vh', backgroundColor: '#020617', color: '#f8fafc' }}>
            <Sidebar />
            <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
                <Header onSearch={setSearchText} />
                <main style={{ padding: '25px', flex: 1, overflowY: 'auto', backgroundColor: '#020617' }}>
                    <Outlet context={[searchText]} />
                </main>
            </div>
        </div>
    );
};
export default MainLayout;