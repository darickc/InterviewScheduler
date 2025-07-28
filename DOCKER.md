# Docker Deployment Guide

This guide explains how to deploy the Interview Scheduler application using Docker.

## Prerequisites

- Docker Engine 20.10 or later
- Docker Compose 2.0 or later
- Google Cloud Console account (for Calendar API)

## Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd InterviewScheduler
   ```

2. **Set up environment variables**
   ```bash
   cp .env.example .env
   # Edit .env file with your Google Calendar API credentials
   ```

3. **Build and run**
   ```bash
   docker-compose up -d
   ```

4. **Access the application**
   - Open http://localhost:8080 in your browser

## Configuration

### Environment Variables

Create a `.env` file from `.env.example` and configure:

- `GOOGLE_CLIENT_ID`: Your Google OAuth 2.0 client ID
- `GOOGLE_CLIENT_SECRET`: Your Google OAuth 2.0 client secret

### Google Calendar API Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable the Google Calendar API
4. Create OAuth 2.0 credentials
5. Add authorized redirect URIs:
   - `http://localhost:8080/auth/google/callback` (development)
   - `https://your-domain.com/auth/google/callback` (production)

## Docker Commands

### Development

```bash
# Build and start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Rebuild after code changes
docker-compose up -d --build
```

### Production with NGINX

```bash
# Start with NGINX reverse proxy
docker-compose --profile production up -d

# This includes:
# - Application container
# - NGINX reverse proxy with SSL support
# - Production optimizations
```

## Data Persistence

The application uses Docker volumes for data persistence:

- `interview_data`: SQLite database and uploaded files
- `./CalendarCredentials`: Google Calendar authentication tokens

## Health Checks

The application includes health checks:

- **Endpoint**: `/health`
- **Docker**: Automatic container health monitoring
- **Interval**: 30 seconds

## SSL/HTTPS Configuration

For production deployment with HTTPS:

1. **Obtain SSL certificates**
   ```bash
   mkdir ssl
   # Copy your cert.pem and key.pem files to ssl/
   ```

2. **Update nginx.conf**
   - Uncomment HTTPS server block
   - Update server_name with your domain

3. **Update docker-compose.yml**
   - Set redirect URI to HTTPS
   - Mount SSL certificates

## Scaling and Production Considerations

### Database

For production, consider using a dedicated database:

```yaml
# Add to docker-compose.yml
services:
  db:
    image: postgres:15
    environment:
      POSTGRES_DB: interviewscheduler
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

### Backup Strategy

```bash
# Backup SQLite database
docker-compose exec interviewscheduler cp /app/data/interviewscheduler.db /app/backup/

# Backup Docker volume
docker run --rm -v interview_data:/data -v $(pwd):/backup alpine tar czf /backup/data-backup.tar.gz -C /data .
```

### Monitoring

Consider adding monitoring services:

```yaml
# Example monitoring stack
services:
  prometheus:
    image: prom/prometheus
    
  grafana:
    image: grafana/grafana
```

## Troubleshooting

### Common Issues

1. **Database locked error**
   ```bash
   # Stop all containers and restart
   docker-compose down
   docker-compose up -d
   ```

2. **Permission denied**
   ```bash
   # Fix volume permissions
   sudo chown -R 1000:1000 ./data
   ```

3. **Port already in use**
   ```bash
   # Change ports in docker-compose.yml
   ports:
     - "8081:8080"  # Use different external port
   ```

### Logs

```bash
# Application logs
docker-compose logs interviewscheduler

# NGINX logs
docker-compose logs nginx

# Follow logs in real-time
docker-compose logs -f
```

## Security Considerations

1. **Environment Variables**: Never commit `.env` file to version control
2. **SSL Certificates**: Use proper SSL certificates in production
3. **Database**: Use strong passwords and limit access
4. **Updates**: Regularly update base images and dependencies
5. **Firewall**: Configure firewall rules for production deployment

## Performance Tuning

### Application

- Adjust connection strings for production database
- Configure caching (Redis/Memory)
- Enable response compression

### Docker

```yaml
# Resource limits
services:
  interviewscheduler:
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'
```

### NGINX

- Enable gzip compression (included in nginx.conf)
- Configure caching headers
- Use HTTP/2 (included in nginx.conf)

## Maintenance

### Updates

```bash
# Pull latest images
docker-compose pull

# Restart with new images
docker-compose up -d

# Clean up old images
docker image prune
```

### Backup

```bash
# Create backup script
cat > backup.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker run --rm -v interview_data:/data -v $(pwd)/backups:/backup alpine tar czf /backup/interview_data_$DATE.tar.gz -C /data .
EOF

chmod +x backup.sh
```