const API_BASE = '/api/employee';

export async function fetchEmployees(page = 1, pageSize = 10) {
    const res = await fetch(`${API_BASE}?page=${page}&pageSize=${pageSize}`);
    if (!res.ok) throw new Error(`Failed to fetch employees: ${res.status}`);
    return res.json();
}

export async function fetchEmployeeByName(name) {
    const res = await fetch(`${API_BASE}/${encodeURIComponent(name)}`);
    if (!res.ok) {
        if (res.status === 404) return null;
        throw new Error(`Failed to fetch employee: ${res.status}`);
    }
    return res.json();
}

export async function uploadEmployees(file, textContent, format) {
    const formData = new FormData();
    if (file) {
        formData.append('file', file);
    }
    if (textContent) {
        formData.append('textContent', textContent);
        formData.append('format', format || 'csv');
    }

    const res = await fetch(API_BASE, { method: 'POST', body: formData });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Upload failed');
    return data;
}
