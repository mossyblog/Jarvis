# PostgreSQL Connection

If you have trouble connecting to PostgreSQL:

- Verify your connection string (Host, Port, Username, Password, Database)
- Check that PostgreSQL Docker container is running: `docker-compose up -d`
- Ensure the database exists and schema matches requirements
- Check PostgreSQL logs: `docker logs pg_graphql_db`
- Verify network connectivity to localhost:5432

## Docker Setup

Start PostgreSQL with:
```bash
docker-compose up -d
```

This uses the `supabase/postgres:15.1.0.155` image which is PostgreSQL with extensions. 