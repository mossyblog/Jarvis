# Integration Tests

This directory contains integration tests that require a real Supabase instance.

## Prerequisites

1. **Local Supabase Instance**: You need Supabase running locally
   ```bash
   supabase start
   ```

2. **Environment Configuration**: Create a `.env.local` file in the repository root with your Supabase settings:
   ```env
   SUPABASE_URL=http://127.0.0.1:54321
   SUPABASE_KEY=your-anon-key
   SUPABASE_SERVICE_KEY=your-service-role-key
   ```

3. **Database Setup**: Run the setup script against your local Supabase:
   ```bash
   # Using Supabase CLI
   supabase db execute -f core.jarvis.tests/Scripts/setup-test-database.sql
   
   # Or using psql directly
   psql postgresql://postgres:postgres@127.0.0.1:54322/postgres -f core.jarvis.tests/Scripts/setup-test-database.sql
   ```

## Running Integration Tests

Integration tests are marked with the `[Collection("SupabaseIntegration")]` attribute and will only run when connected to a local Supabase instance.

```bash
# Run all tests including integration tests
dotnet test

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run with detailed output
dotnet test --filter "FullyQualifiedName~Integration" -v normal
```

## Test Categories

- **ServiceRegistrationIntegrationTests**: Tests DI container setup and service resolution
- **AuditServiceIntegrationTests**: Tests audit trail persistence to Supabase
- **StorageIntegrationTests**: (Future) Tests entity/component storage operations

## Notes

- Integration tests automatically skip if not connected to a local Supabase instance
- Tests clean up their data after execution
- Tests in the same collection run sequentially to avoid conflicts
- Use the service role key for tests to bypass RLS