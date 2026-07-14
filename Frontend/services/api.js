// Auto-detect environment: use local backend in development, Render in production
const IS_LOCAL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
const API_BASE_URL = IS_LOCAL
    ? 'http://localhost:5260/api'
    : 'https://ai-talent-hub-api.onrender.com/api';

async function request(endpoint, options = {}) {
    const token = localStorage.getItem('token');
    
    const headers = {
        'Content-Type': 'application/json',
        ...(options.headers || {})
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const config = {
        ...options,
        headers
    };

    try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, config);
        
        if (response.status === 401) {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            if (!window.location.pathname.includes('auth.html') && !window.location.pathname.includes('index.html')) {
                window.location.href = '/pages/auth.html';
            }
            throw new Error('Unauthorized');
        }

        // Handle response body - safely parse JSON or fall back to plain text
        const text = await response.text();
        let data;
        try {
            data = text ? JSON.parse(text) : {};
        } catch (e) {
            // Response is plain text, not JSON
            data = text;
        }
        
        if (!response.ok) {
            // Extract a readable error message from various backend response formats
            let errorMsg = 'Something went wrong';
            if (typeof data === 'string') {
                errorMsg = data;
            } else if (data.title) {
                errorMsg = data.title;
                // Check for validation errors object
                if (data.errors) {
                    const msgs = Object.values(data.errors).flat();
                    if (msgs.length > 0) errorMsg = msgs.join(' ');
                }
            } else if (data.message) {
                errorMsg = data.message;
            }
            throw new Error(errorMsg);
        }
        
        return data;
    } catch (error) {
        console.error(`API Error on ${endpoint}:`, error);
        throw error;
    }
}

const api = {
    get: (endpoint, options) => request(endpoint, { ...options, method: 'GET' }),
    post: (endpoint, body, options) => request(endpoint, { ...options, method: 'POST', body: JSON.stringify(body) }),
    put: (endpoint, body, options) => request(endpoint, { ...options, method: 'PUT', body: JSON.stringify(body) }),
    delete: (endpoint, options) => request(endpoint, { ...options, method: 'DELETE' })
};
