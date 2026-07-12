const authService = {
    async login(email, password) {
        try {
            const data = await api.post('/auth/login', { email, password });
            if (data.token) {
                localStorage.setItem('token', data.token);
                localStorage.setItem('user', JSON.stringify(data.user));
            }
            return data;
        } catch (error) {
            throw error;
        }
    },

    async register(email, password, fullName, role, companyName = '') {
        try {
            const data = await api.post('/auth/register', {
                email,
                password,
                fullName,
                role,
                companyName
            });
            if (data.token) {
                localStorage.setItem('token', data.token);
                localStorage.setItem('user', JSON.stringify(data.user));
            }
            return data;
        } catch (error) {
            throw error;
        }
    },

    logout() {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        window.location.href = '/index.html';
    },

    getCurrentUser() {
        const userJson = localStorage.getItem('user');
        if (!userJson) return null;
        try {
            return JSON.parse(userJson);
        } catch (e) {
            return null;
        }
    },

    isAuthenticated() {
        return localStorage.getItem('token') !== null;
    }
};
