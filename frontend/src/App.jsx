import { useState, useEffect, useCallback } from 'react';
import EmployeeList from './components/EmployeeList';
import EmployeeDetail from './components/EmployeeDetail';
import UploadForm from './components/UploadForm';
import Toast from './components/Toast';
import { fetchEmployees } from './api';

export default function App() {
    const [tab, setTab] = useState('list');
    const [employees, setEmployees] = useState([]);
    const [page, setPage] = useState(1);
    const [pageSize] = useState(10);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(0);
    const [loading, setLoading] = useState(false);
    const [selectedName, setSelectedName] = useState(null);
    const [toast, setToast] = useState(null);

    const loadEmployees = useCallback(async () => {
        setLoading(true);
        try {
            const data = await fetchEmployees(page, pageSize);
            setEmployees(data.items || []);
            setTotalCount(data.totalCount || 0);
            setTotalPages(data.totalPages || 0);
        } catch (err) {
            setToast({ type: 'error', message: err.message });
        } finally {
            setLoading(false);
        }
    }, [page, pageSize]);

    useEffect(() => {
        loadEmployees();
    }, [loadEmployees]);

    const handleSelectEmployee = (name) => {
        setSelectedName(name);
        setTab('detail');
    };

    const handleBack = () => {
        setSelectedName(null);
        setTab('list');
    };

    const handleUploadSuccess = (msg) => {
        setToast({ type: 'success', message: msg });
        setPage(1);
        loadEmployees();
        setTab('list');
    };

    const showToast = (type, message) => {
        setToast({ type, message });
    };

    return (
        <div className="app">
            <header className="header">
                <h1>Employee Contact Manager</h1>
                <p>Manage and browse your employee directory</p>
            </header>

            {tab !== 'detail' && (
                <nav className="tabs">
                    <button
                        className={`tab-btn ${tab === 'list' ? 'active' : ''}`}
                        onClick={() => setTab('list')}
                    >
                        📋 Employee List
                    </button>
                    <button
                        className={`tab-btn ${tab === 'upload' ? 'active' : ''}`}
                        onClick={() => setTab('upload')}
                    >
                        📤 Upload Data
                    </button>
                </nav>
            )}

            {tab === 'list' && (
                <EmployeeList
                    employees={employees}
                    loading={loading}
                    page={page}
                    totalPages={totalPages}
                    totalCount={totalCount}
                    onPageChange={setPage}
                    onSelect={handleSelectEmployee}
                />
            )}

            {tab === 'detail' && (
                <EmployeeDetail
                    name={selectedName}
                    onBack={handleBack}
                    onError={(msg) => showToast('error', msg)}
                />
            )}

            {tab === 'upload' && (
                <UploadForm
                    onSuccess={handleUploadSuccess}
                    onError={(msg) => showToast('error', msg)}
                />
            )}

            {toast && (
                <Toast
                    type={toast.type}
                    message={toast.message}
                    onClose={() => setToast(null)}
                />
            )}
        </div>
    );
}
