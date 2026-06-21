const jobsService = {
    // Job Postings APIs
    async getAllJobs(search = '', location = '', jobType = '') {
        const queryParams = new URLSearchParams();
        if (search) queryParams.append('search', search);
        if (location) queryParams.append('location', location);
        if (jobType) queryParams.append('jobType', jobType);
        
        const queryString = queryParams.toString() ? `?${queryParams.toString()}` : '';
        return await api.get(`/jobs${queryString}`);
    },

    async getJobDetails(id) {
        return await api.get(`/jobs/${id}`);
    },

    async getMyPostings() {
        return await api.get('/jobs/my-postings');
    },

    async createJob(jobData) {
        return await api.post('/jobs', jobData);
    },

    async updateJob(id, jobData) {
        return await api.put(`/jobs/${id}`, jobData);
    },

    async deleteJob(id) {
        return await api.delete(`/jobs/${id}`);
    },

    async getRecommendations() {
        return await api.get('/jobs/recommendations');
    },

    // Applications APIs
    async applyForJob(applicationData) {
        return await api.post('/applications/apply', applicationData);
    },

    async uploadResumeFile(file) {
        const formData = new FormData();
        formData.append('file', file);
        const token = localStorage.getItem('token');
        const headers = {};
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        const response = await fetch(`${API_BASE_URL}/resumes/upload`, {
            method: 'POST',
            headers,
            body: formData
        });
        if (!response.ok) {
            const errText = await response.text();
            let errorMsg = 'Failed to upload resume file';
            try {
                const parsed = JSON.parse(errText);
                if (parsed.message) errorMsg = parsed.message;
            } catch (e) {
                if (errText) errorMsg = errText;
            }
            throw new Error(errorMsg);
        }
        return await response.json();
    },

    async updateInterviewFeedback(interviewId, feedback, rating) {
        return await api.put(`/applications/interview/${interviewId}/feedback`, {
            feedback,
            candidateRating: parseInt(rating)
        });
    },

    async getMyApplications() {
        return await api.get('/applications/my-applications');
    },

    async getJobApplicants(jobId) {
        return await api.get(`/applications/job/${jobId}`);
    },

    async updateApplicationStatus(appId, status) {
        return await api.put(`/applications/${appId}/status`, { status });
    },

    async scheduleInterview(appId, scheduledTime, locationOrLink, notes) {
        return await api.post(`/applications/${appId}/schedule-interview`, {
            scheduledTime,
            locationOrLink,
            notes
        });
    },

    async getMyInterviews() {
        return await api.get('/applications/my-interviews');
    },

    // Resume / Candidate Profile APIs
    async getMyProfile() {
        return await api.get('/resumes/my-profile');
    },

    async updateMyProfile(profileData) {
        return await api.put('/resumes/my-profile', profileData);
    },

    async parseResumeText(resumeText) {
        return await api.post('/resumes/parse-text', { resumeText });
    },

    // Recruiter Profile APIs
    async getRecruiterProfile() {
        return await api.get('/profile/recruiter');
    },

    async updateRecruiterProfile(profileData) {
        return await api.put('/profile/recruiter', profileData);
    },

    // Admin / Hiring Manager APIs
    async getAdminStats() {
        return await api.get('/admin/stats');
    },

    async getAllUsers(role = '') {
        const query = role ? `?role=${role}` : '';
        return await api.get(`/admin/users${query}`);
    },

    async updateUserRole(userId, role) {
        return await api.put(`/admin/users/${userId}/role`, { role });
    },

    async deleteUser(userId) {
        return await api.delete(`/admin/users/${userId}`);
    },

    async adminGetAllJobs() {
        return await api.get('/admin/jobs');
    },

    async adminDeleteJob(jobId) {
        return await api.delete(`/admin/jobs/${jobId}`);
    }
};
