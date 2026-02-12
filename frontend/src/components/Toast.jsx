import { useEffect } from 'react';

export default function Toast({ type, message, onClose }) {
    useEffect(() => {
        const timer = setTimeout(onClose, 3000);
        return () => clearTimeout(timer);
    }, [onClose]);

    return (
        <div className={`toast ${type}`} onClick={onClose}>
            {type === 'success' ? '✓ ' : '✖ '}
            {message}
        </div>
    );
}
