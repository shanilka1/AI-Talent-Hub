const candidatesService = {
    searchCandidates: async (query = '') => {
        return await api.get(`/Profile/candidates/search?query=${encodeURIComponent(query)}`);
    }
};
