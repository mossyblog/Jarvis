# Database Setup Guide

## Quick Start (Docker - Recommended)

**Fully automated setup** - no manual steps required:

```bash
# Start fresh database with automatic initialization
docker-compose up -d
```

The database will be automatically initialized with tables, default user, and navigation items!

## Alternative: Manual Setup

If you prefer manual control or need to reinitialize:

```bash
./setup.sh
```

This script is **idempotent** - you can run it multiple times safely.

## What It Does

The `setup.sh` script automatically:

1. ✅ **Creates all required tables** with proper indexes
2. ✅ **Sets up default test user**: `test@example.com` / `TestPassword123!`
3. ✅ **Creates navigation items** for the main menu
4. ✅ **Configures GraphQL extension** (if available)
5. ✅ **Handles conflicts** - safe to run multiple times

## Default Test Account

- **Email**: `test@example.com`
- **Password**: `TestPassword123!`
- **Active**: Yes
- **Method**: Password authentication

## Environment Variables

You can customize the database connection:

```bash
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME=jarvis_test
export DB_USER=postgres
export DB_PASSWORD=postgres
./setup.sh
```

## After Database Recreation

### Docker Method (Recommended)
```bash
# Stop and remove all data
docker-compose down -v

# Start fresh - automatic initialization
docker-compose up -d

# Start API and UI
func start --port 7071    # In one terminal
npm run dev               # In another terminal
```

### Manual Method
1. **Stop the API** if running
2. **Drop/recreate database** as needed  
3. **Run setup**: `./setup.sh`
4. **Start API**: `func start --port 7071`
5. **Start UI**: `npm run dev`

The login form will automatically use the test credentials, so you can immediately log in and see the navigation working.

## Docker Initialization Details

The Docker setup includes automatic initialization via `/docker-init/` directory:

- **`01-init.sh`**: Main initialization script with progress logging
- **`02-init.sql`**: Pure SQL alternative for faster execution
- **Runs automatically** on first container startup
- **Idempotent**: Safe to run multiple times
- **See `/docker-init/README.md`** for complete details

## Troubleshooting

- **Container not running**: Ensure `docker-compose up -d` is running first
- **Permission errors**: Check Docker container has proper PostgreSQL setup
- **GraphQL issues**: The script handles missing pg_graphql extension gracefully

## Production Notes

In production environments:
- Change default passwords immediately
- Use secure environment variables for DB credentials
- Consider using database migrations instead of this setup script