# Database Setup Guide

## Quick Start

After dropping and recreating the database, simply run:

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

1. **Stop the API** if running
2. **Drop/recreate database** as needed
3. **Run setup**: `./setup.sh`
4. **Start API**: `func start --port 7071`
5. **Start UI**: `npm run dev`

The login form will automatically use the test credentials, so you can immediately log in and see the navigation working.

## Troubleshooting

- **Container not running**: Ensure `docker-compose up -d` is running first
- **Permission errors**: Check Docker container has proper PostgreSQL setup
- **GraphQL issues**: The script handles missing pg_graphql extension gracefully

## Production Notes

In production environments:
- Change default passwords immediately
- Use secure environment variables for DB credentials
- Consider using database migrations instead of this setup script