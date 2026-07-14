const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 3000;
const PUBLIC_DIR = path.join(__dirname, 'Frontend');

const MIME_TYPES = {
    '.html': 'text/html',
    '.css': 'text/css',
    '.js': 'text/javascript',
    '.json': 'application/json',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.gif': 'image/gif',
    '.svg': 'image/svg+xml',
    '.ico': 'image/x-icon'
};

const server = http.createServer((req, res) => {
    // Parse URL and strip query strings
    const parsedUrl = new URL(req.url, `http://localhost:${PORT}`);
    let reqPath = parsedUrl.pathname;
    
    let filePath = path.join(PUBLIC_DIR, reqPath === '/' ? 'index.html' : reqPath);
    const extname = path.extname(filePath);
    let contentType = MIME_TYPES[extname] || 'application/octet-stream';

    // Prevent directory traversal attacks
    if (!filePath.startsWith(PUBLIC_DIR)) {
        res.writeHead(403, { 'Content-Type': 'text/plain' });
        res.end('Forbidden');
        return;
    }

    fs.readFile(filePath, (error, content) => {
        if (error) {
            if (error.code === 'ENOENT') {
                // If file not found, check if user requested a page without .html extension
                if (!extname) {
                    const htmlPath = filePath + '.html';
                    fs.readFile(htmlPath, (htmlErr, htmlContent) => {
                        if (htmlErr) {
                            serve404(res);
                        } else {
                            res.writeHead(200, { 'Content-Type': 'text/html' });
                            res.end(htmlContent, 'utf-8');
                        }
                    });
                } else {
                    serve404(res);
                }
            } else {
                res.writeHead(500, { 'Content-Type': 'text/plain' });
                res.end(`Server Error: ${error.code}`);
            }
        } else {
            res.writeHead(200, { 'Content-Type': contentType });
            res.end(content, 'utf-8');
        }
    });
});

function serve404(res) {
    fs.readFile(path.join(PUBLIC_DIR, 'index.html'), (err, content) => {
        if (err) {
            res.writeHead(404, { 'Content-Type': 'text/plain' });
            res.end('404 Page Not Found');
        } else {
            res.writeHead(404, { 'Content-Type': 'text/html' });
            res.end(content, 'utf-8');
        }
    });
}

server.listen(PORT, () => {
    console.log(`==================================================`);
    console.log(`🚀 AI Talent Hub Frontend server running!`);
    console.log(`👉 Open: http://localhost:${PORT}`);
    console.log(`==================================================`);
});
