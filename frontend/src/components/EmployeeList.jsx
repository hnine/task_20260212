export default function EmployeeList({ employees, loading, page, totalPages, totalCount, onPageChange, onSelect }) {
    if (loading) {
        return <div className="spinner" />;
    }

    if (!employees || employees.length === 0) {
        return (
            <div className="card empty-state">
                <span>📭</span>
                <p>No employees found. Upload some data to get started!</p>
            </div>
        );
    }

    return (
        <div>
            <div className="employee-grid">
                {employees.map((emp, i) => (
                    <div
                        className="employee-card"
                        key={`${emp.name}-${i}`}
                        onClick={() => onSelect(emp.name)}
                    >
                        <div className="employee-card-avatar">
                            {getInitials(emp.name)}
                        </div>
                        <div className="employee-card-info">
                            <h3 className="employee-card-name">{emp.name}</h3>
                            <div className="employee-card-detail">
                                <span className="employee-card-icon">✉</span>
                                <span>{emp.email}</span>
                            </div>
                            <div className="employee-card-detail">
                                <span className="employee-card-icon">📞</span>
                                <span>{emp.telNumber}</span>
                            </div>
                            <div className="employee-card-detail">
                                <span className="employee-card-icon">📅</span>
                                <span>{formatDate(emp.joinedDate)}</span>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            <div className="pagination">
                <button disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
                    ← Prev
                </button>
                <span className="page-info">
                    Page {page} of {totalPages} ({totalCount} total)
                </span>
                <button disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
                    Next →
                </button>
            </div>
        </div>
    );
}

function getInitials(name) {
    if (!name) return '?';
    return name
        .split(' ')
        .map(word => word[0])
        .join('')
        .toUpperCase()
        .slice(0, 2);
}

function formatDate(dateStr) {
    if (!dateStr) return '—';
    try {
        return new Date(dateStr).toLocaleDateString('en-CA');
    } catch {
        return dateStr;
    }
}
