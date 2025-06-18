# API Configuration

## Database Connection

The API uses PostgreSQL for data storage. Configure your database connection in `local.settings.json`:

```json
{
  "ConnectionStrings": {
    "JarvisDb": "Host=localhost;Port=5432;Database=jarvis;Username=postgres;Password=postgres"
  }
}
```

### Connection String Parameters:
- **Host**: Your PostgreSQL server hostname (e.g., `localhost`, `127.0.0.1`, or remote server)
- **Port**: PostgreSQL port (default: `5432`)
- **Database**: Database name (e.g., `jarvis`)
- **Username**: PostgreSQL username
- **Password**: PostgreSQL password

### Example Connection Strings:

**Local PostgreSQL:**
```
Host=localhost;Port=5432;Database=jarvis;Username=postgres;Password=mypassword
```

**Remote PostgreSQL:**
```
Host=db.myserver.com;Port=5432;Database=jarvis;Username=jarvis_user;Password=secure_password;SSL Mode=Require
```

**PostgreSQL with SSL:**
```
Host=myserver.postgres.database.azure.com;Port=5432;Database=jarvis;Username=myuser@myserver;Password=mypass;SSL Mode=Require;Trust Server Certificate=true
```

## JWT Configuration

Configure JWT settings for authentication:

```json
{
  "Values": {
    "Jwt:Issuer": "jarvis-api",
    "Jwt:Audience": "jarvis-clients",
    "Jwt:SecretKey": "YOUR_SECRET_KEY_HERE_MINIMUM_256_BITS",
    "Jwt:AccessTokenExpirationMinutes": "15",
    "Jwt:RefreshTokenExpirationDays": "30"
  }
}
```

## Running the API

1. Ensure PostgreSQL is running and accessible
2. Update `local.settings.json` with your database connection details
3. Run the API:
   ```bash
   func start
   ```

## Troubleshooting

### Connection String Not Found
If you see "No Connection String" error, ensure:
1. `local.settings.json` exists in the API project root
2. The connection string is under `ConnectionStrings.JarvisDb`
3. The file is properly formatted JSON

### Database Connection Failed
If the API can't connect to the database:
1. Verify PostgreSQL is running: `psql -U postgres -c "SELECT 1"`
2. Check the connection parameters match your PostgreSQL configuration
3. Ensure the database exists: `createdb jarvis` (if needed)
4. Check firewall/network settings if using a remote database