import { useState, useRef } from 'react';
import { uploadEmployees } from '../api';

export default function UploadForm({ onSuccess, onError }) {
    const [file, setFile] = useState(null);
    const [textContent, setTextContent] = useState('');
    const [format, setFormat] = useState('csv');
    const [submitting, setSubmitting] = useState(false);
    const [dragover, setDragover] = useState(false);
    const fileInputRef = useRef(null);

    const handleDrop = (e) => {
        e.preventDefault();
        setDragover(false);
        const dropped = e.dataTransfer.files[0];
        if (dropped) setFile(dropped);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!file && !textContent.trim()) {
            onError('Please upload a file or enter text content.');
            return;
        }

        setSubmitting(true);
        try {
            const data = await uploadEmployees(
                file,
                textContent.trim() || null,
                format
            );
            setFile(null);
            setTextContent('');
            if (fileInputRef.current) fileInputRef.current.value = '';
            onSuccess(data.message || 'Employees uploaded successfully!');
        } catch (err) {
            onError(err.message);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <form className="card upload-form" onSubmit={handleSubmit}>
            {/* File Upload */}
            <div className="form-group">
                <label>Upload File (CSV or JSON)</label>
                <div
                    className={`file-drop ${dragover ? 'dragover' : ''}`}
                    onClick={() => fileInputRef.current?.click()}
                    onDragOver={(e) => { e.preventDefault(); setDragover(true); }}
                    onDragLeave={() => setDragover(false)}
                    onDrop={handleDrop}
                >
                    <div className="icon">📁</div>
                    {file ? (
                        <p className="selected-file">✓ {file.name}</p>
                    ) : (
                        <p>Drop a file here or click to browse</p>
                    )}
                </div>
                <input
                    ref={fileInputRef}
                    type="file"
                    accept=".csv,.json"
                    style={{ display: 'none' }}
                    onChange={(e) => setFile(e.target.files[0] || null)}
                />
            </div>

            <div className="divider">or</div>

            {/* Text Input */}
            <div className="form-group">
                <label>Paste Employee Data</label>
                <div className="format-select">
                    <button
                        type="button"
                        className={`format-btn ${format === 'csv' ? 'active' : ''}`}
                        onClick={() => setFormat('csv')}
                    >
                        CSV
                    </button>
                    <button
                        type="button"
                        className={`format-btn ${format === 'json' ? 'active' : ''}`}
                        onClick={() => setFormat('json')}
                    >
                        JSON
                    </button>
                </div>
                <textarea
                    value={textContent}
                    onChange={(e) => setTextContent(e.target.value)}
                    placeholder={
                        format === 'csv'
                            ? 'name, email, tel number, joined date\nJohn Doe, john@example.com, 010-1234-5678, 2024.01.15'
                            : '[{"name":"John Doe","email":"john@example.com","tel":"010-1234-5678","joined":"2024-01-15"}]'
                    }
                />
            </div>

            <button type="submit" className="submit-btn" disabled={submitting}>
                {submitting ? 'Uploading…' : '🚀 Upload Employees'}
            </button>
        </form>
    );
}
