const API_BASE_URL = 'http://localhost:5260/api';

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

        // Handle empty response bodies
        const text = await response.text();
        const data = text ? JSON.parse(text) : {};
        
        if (!response.ok) {
            throw new Error(data.message || (typeof data === 'string' ? data : JSON.stringify(data)) || 'Something went wrong');
        }
        
        return data;
    } catch (error) {
        console.error(`API Error on ${endpoint}:`, error);
        throw error;
    }
}

const api = {
    get: (endpoint, options) => request(endpoint, { ...options, method: 'GET' }),
    post: (endpoint, body, options) => request(endpoint, { ...options, method: 'POST', body: body }),
    put: (endpoint, body, options) => request(endpoint, { ...options, method: 'PUT', body: body }),
    delete: (endpoint, options) => request(endpoint, { ...options, method: 'DELETE' })
};
