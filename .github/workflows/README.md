# GitHub Actions CI/CD Pipeline

This directory contains the GitHub Actions workflows for the Jarvis framework.

## Workflow: dotnet.yml

The main CI/CD pipeline that builds and tests all projects in the solution.

### Triggers
- **Push**: Runs on pushes to the `master` branch
- **Pull Request**: Runs on pull requests targeting the `master` branch

### Environment Setup

#### PostgreSQL Service
The workflow includes a PostgreSQL 15 service container with:
- **Database**: `jarvis_test`
- **Username**: `supabase_admin`
- **Password**: `postgres`
- **Port**: 5432

The database is automatically available to all test steps via the `TEST_DATABASE_URL` environment variable.

#### .NET SDK
- **Version**: 8.0.x (matches project target framework)

### Pipeline Steps

1. **Checkout** - Fetches the repository code
2. **Setup .NET** - Installs .NET 8.0 SDK
3. **Restore** - Restores NuGet packages
4. **Build** - Builds all projects in the solution
5. **Test** - Runs all tests with code coverage collection
6. **Upload Coverage** - Sends coverage reports to Codecov

### Database Setup

The test infrastructure automatically handles database setup:
1. `TestDatabaseSetup.cs` reads the `TEST_DATABASE_URL` environment variable
2. It executes `setup-test-database.sql` to create all required tables
3. A test user is created: `test@example.com` / `test123`

### Local Testing

To replicate the CI environment locally:

```bash
# Start PostgreSQL container
docker run -d \
  --name jarvis-test-db \
  -e POSTGRES_USER=supabase_admin \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=jarvis_test \
  -p 5432:5432 \
  postgres:15

# Set environment variable
export TEST_DATABASE_URL="Host=localhost;Port=5432;Database=jarvis_test;Username=supabase_admin;Password=postgres"

# Run tests
dotnet test
```

### Troubleshooting

#### Database Connection Issues
- Ensure the PostgreSQL service is healthy before tests run
- Check that `TEST_DATABASE_URL` is properly formatted
- Verify firewall/network settings allow localhost connections

#### Test Failures
- Integration tests require the database to be available
- Unit tests should not require database access
- Check test logs for specific error messages

#### Coverage Reports
- Coverage files are generated in `TestResults/**/coverage.cobertura.xml`
- Codecov requires the `CODECOV_TOKEN` secret to be configured

### Future Enhancements

Consider adding:
- Matrix builds for multiple OS testing (Windows, macOS)
- Separate jobs for unit vs integration tests
- NuGet package caching for faster builds
- Artifact upload for test results and binaries
- Deployment steps for releases