# Docker Database Initialization

This directory contains scripts that automatically initialize the Jarvis PostgreSQL database when the Docker container starts **for the first time**.

## How It Works

PostgreSQL Docker containers automatically execute files in `/docker-entrypoint-initdb.d/` during initialization:

1. **Files are executed in alphabetical order**
2. **Only runs on first container startup** (when data volume is empty)
3. **Supports both `.sql` and `.sh` files**

## Files

### `01-init.sh` 
**Main initialization script** with progress logging and error handling:
- Creates all required tables with indexes
- Seeds default test user: `test@example.com` / `TestPassword123!`
- Creates navigation items for UI
- Sets up GraphQL extension (if available)
- Provides detailed progress feedback

### `02-init.sql`
**Pure SQL alternative** for environments that prefer SQL-only initialization:
- Same functionality as shell script
- Faster execution (no shell overhead)
- More portable across different environments
- Easier to version control and review

## Usage

### Automatic Initialization (Recommended)

```bash
# Start fresh database (first time)
docker-compose up -d

# Database will be automatically initialized with:
# ✅ All tables and indexes
# ✅ Default test user (test@example.com / TestPassword123!)  
# ✅ Navigation items
# ✅ GraphQL setup
```

### Force Re-initialization

```bash
# Stop and remove volumes (destroys all data!)
docker-compose down -v

# Start fresh - initialization will run again
docker-compose up -d
```

### Manual Testing

```bash
# Test shell script directly
docker exec jarvis-postgres bash /docker-entrypoint-initdb.d/01-init.sh

# Test SQL script directly  
docker exec jarvis-postgres psql -U postgres -d jarvis_test -f /docker-entrypoint-initdb.d/02-init.sql
```

## Environment Variables

The initialization scripts respect these environment variables from docker-compose.yml:

- `POSTGRES_DB=jarvis_test` - Database name to create
- `POSTGRES_USER=postgres` - Database user
- `POSTGRES_PASSWORD=postgres` - Database password

## Default Data Created

### Test User
- **Email**: `test@example.com`
- **Password**: `TestPassword123!` (bcrypt hashed)
- **Status**: Active
- **Method**: Password authentication

### Navigation Items
- **Dashboard** → `/` (LayoutDashboard icon)
- **Accounts** → `/accounts` (Users icon)  
- **Schema** → `/schema` (Database icon)

## Troubleshooting

### Scripts Don't Run
- **Check volumes**: Ensure `./docker-init:/docker-entrypoint-initdb.d:ro` is mounted
- **Check permissions**: Scripts need execute permissions (`chmod +x docker-init/*.sh`)
- **Check timing**: Scripts only run on **first startup** with empty data volume

### Database Not Ready
- **Check logs**: `docker-compose logs postgres`
- **Wait for ready**: Look for "database system is ready to accept connections"
- **Manual verification**: `docker exec jarvis-postgres psql -U postgres -d jarvis_test -c "\dt"`

### Missing Tables/Data
- **Force reinit**: `docker-compose down -v && docker-compose up -d`
- **Check scripts**: Verify scripts are error-free with `set -e`
- **Manual run**: Execute scripts directly in container

## Migration from setup.sh

The old manual `setup.sh` script is still available for backward compatibility, but the Docker approach is recommended:

| Method | Pros | Cons |
|--------|------|------|
| **Docker Init** | ✅ Fully automated<br>✅ Version controlled<br>✅ Consistent | ❌ Only on first startup |
| **setup.sh** | ✅ Run anytime<br>✅ Good for development | ❌ Manual execution<br>❌ Easy to forget |

## Production Notes

- **Change default passwords** immediately in production
- **Use secrets management** for sensitive configuration  
- **Consider database migrations** for production schema changes
- **Test initialization** in staging environments first