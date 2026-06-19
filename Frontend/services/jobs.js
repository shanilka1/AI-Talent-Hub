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
    async applyForJob(jobPostId) {
        return await api.post('/applications/apply', { jobPostId });
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
    }
};
