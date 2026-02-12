import { useState, useEffect } from 'react';
import { fetchEmployeeByName } from '../api';

export default function EmployeeDetail({ name, onBack, onError }) {
    const [employee, setEmployee] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            setLoading(true);
            try {
                const data = await fetchEmployeeByName(name);
                if (cancelled) return;
                if (!data) {
                    onError(`Employee "${name}" not found.`);
                    onBack();
                    return;
                }
                setEmployee(data);
            } catch (err) {
                if (!cancelled) onError(err.message);
            } finally {
                if (!cancelled) setLoading(false);
            }
        }

        load();
        return () => { cancelled = true; };
    }, [name]);

    if (loading) return <div className="spinner" />;
    if (!employee) return null;

    return (
        <div className="detail-panel">
            <button className="back-btn" onClick={onBack}>
                ← Back to List
            </button>

            <div className="card">
                <div className="detail-fields">
                    <div className="detail-field">
                        <label>Full Name</label>
                        <div className="value">{employee.name}</div>
                    </div>
                    <div className="detail-field">
                        <label>Email Address</label>
                        <div className="value">{employee.email}</div>
                    </div>
                    <div className="detail-field">
                        <label>Telephone Number</label>
                        <div className="value">{employee.telNumber}</div>
                    </div>
                    <div className="detail-field">
                        <label>Joined Date</label>
                        <div className="value">{formatDate(employee.joinedDate)}</div>
                    </div>
                </div>
            </div>
        </div>
    );
}

function formatDate(dateStr) {
    if (!dateStr) return '—';
    try {
        return new Date(dateStr).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
        });
    } catch {
        return dateStr;
    }
}
