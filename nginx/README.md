# Magenta CDN Setup

This directory contains the nginx configuration and static files for the Magenta CDN.

## Structure

```
nginx/
├── conf.d/
│   └── cdn.conf          # Nginx configuration
├── html/                 # Static files served by CDN
│   ├── index.html        # CDN landing page
│   └── images/           # Static images directory
└── README.md            # This file
```

## Services

### Nginx CDN (Port 8080)
- Serves static files with optimized caching
- Gzip compression for text files
- CORS headers for cross-origin requests
- Long-term caching (1 year) for static assets
- Directory listing enabled for easy browsing
- Health check endpoint at `/healthz`

## Usage

1. Start the services:
   ```bash
   docker-compose up -d
   ```

2. Access the CDN: http://localhost:8080
3. Browse files: http://localhost:8080/images/

## File Management

### Adding Files
1. Copy files directly to the `nginx/html/` folder
2. Create subfolders as needed (e.g., `nginx/html/css/`, `nginx/html/js/`)
3. Files are immediately available at http://localhost:8080/filename.ext

### Example Structure
```
nginx/html/
├── index.html
├── images/
│   ├── logo.svg
│   ├── banner.jpg
│   └── icons/
│       └── home.svg
├── css/
│   └── styles.css
└── js/
    └── app.js
```

### Accessing Files
- Main page: http://localhost:8080
- Images: http://localhost:8080/images/logo.svg
- CSS: http://localhost:8080/css/styles.css
- JS: http://localhost:8080/js/app.js
- Directory listing: http://localhost:8080/images/

## Configuration

The nginx configuration is in `conf.d/cdn.conf` and includes:
- Static file serving from `/usr/share/nginx/html`
- Long-term caching for static assets (1 year)
- Gzip compression for text files
- CORS headers for cross-origin requests
- Security headers
- Directory listing for easy file browsing
- Health check endpoint
