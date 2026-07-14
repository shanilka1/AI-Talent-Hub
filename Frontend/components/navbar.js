document.addEventListener('DOMContentLoaded', () => {
    renderNavbar();
});

function renderNavbar() {
    const header = document.querySelector('header');
    if (!header) return;

    const token = localStorage.getItem('token');
    const userJson = localStorage.getItem('user');
    let user = null;
    if (userJson) {
        try {
            user = JSON.parse(userJson);
        } catch (e) {
            console.error("Error parsing user info", e);
        }
    }

    let navLinksHtml = `
        <li><a href="/index.html" id="nav-home">Home</a></li>
    `;

    if (!token || !user || user.role === 'Candidate') {
        navLinksHtml += `
            <li><a href="/pages/job-search.html" id="nav-jobs">Find Jobs</a></li>
        `;
    }

    if (token && user) {
        if (user.role === 'Candidate') {
            navLinksHtml += `
                <li><a href="/pages/candidate-dashboard.html" id="nav-dashboard">Candidate Dashboard</a></li>
                <li><a href="/pages/resume-builder.html" id="nav-resume">AI Resume Builder</a></li>
            `;
        } else if (user.role === 'Recruiter') {
            navLinksHtml += `
                <li><a href="/pages/recruiter-dashboard.html" id="nav-dashboard">Recruiter Dashboard</a></li>
                <li><a href="/pages/post-job.html" id="nav-post-job">Post a Job</a></li>
            `;
        } else if (user.role === 'HiringManager') {
            navLinksHtml += `
                <li><a href="/pages/hiring-manager-dashboard.html" id="nav-dashboard">Hiring Manager Dashboard</a></li>
            `;
        } else if (user.role === 'Admin') {
            navLinksHtml += `
                <li><a href="/pages/admin-dashboard.html" id="nav-dashboard">Admin Dashboard</a></li>
            `;
        }
        
        // Add profile link and logout button
        navLinksHtml += `
            <li><a href="/pages/profile.html" id="nav-profile">Profile</a></li>
            <li><a href="#" id="nav-logout" class="btn btn-secondary" style="padding: 0.4rem 0.8rem; font-size: 0.85rem;">Logout</a></li>
        `;
    } else {
        navLinksHtml += `
            <li><a href="/pages/auth.html" id="nav-login" class="btn btn-nav-action" style="padding: 0.5rem 1.2rem;">Login / Register</a></li>
        `;
    }

    header.innerHTML = `
        <div class="nav-container">
            <a href="/index.html" class="logo">
                ✨ AI Talent Hub
            </a>
            <nav>
                <ul class="nav-links">
                    ${navLinksHtml}
                </ul>
            </nav>
        </div>
    `;

    // Setup active state highlighting based on path
    const path = window.location.pathname;
    const links = {
        'index.html': 'nav-home',
        '/': 'nav-home',
        'job-search.html': 'nav-jobs',
        'candidate-dashboard.html': 'nav-dashboard',
        'recruiter-dashboard.html': 'nav-dashboard',
        'hiring-manager-dashboard.html': 'nav-dashboard',
        'admin-dashboard.html': 'nav-dashboard',
        'resume-builder.html': 'nav-resume',
        'post-job.html': 'nav-post-job',
        'profile.html': 'nav-profile',
        'auth.html': 'nav-login'
    };

    for (const [key, value] of Object.entries(links)) {
        if (path.includes(key)) {
            const el = document.getElementById(value);
            if (el) el.classList.add('active');
        }
    }

    // Setup logout handler
    const logoutBtn = document.getElementById('nav-logout');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            window.location.href = '/index.html';
        });
    }
}
