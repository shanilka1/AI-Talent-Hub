document.addEventListener('DOMContentLoaded', () => {
    const footer = document.querySelector('footer');
    if (!footer) return;
    
    footer.innerHTML = `
        <div class="footer-content">
            <p>&copy; 2026 AI Talent Hub. Connecting Talent with Opportunities Through Artificial Intelligence.</p>
            <p style="margin-top: 0.5rem; font-size: 0.8rem; color: #4b5563;">Built for smart matching and interactive resume generation.</p>
        </div>
    `;
});
