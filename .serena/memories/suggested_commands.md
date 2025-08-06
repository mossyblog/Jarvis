# Suggested Commands for Jarvis Development

## Build Commands
```bash
# Build all projects
dotnet build

# Build specific project
dotnet build core.jarvis/core.jarvis.csproj
```

## Test Commands
```bash
# Run all backend tests
dotnet test

# Run specific test project
dotnet test core.jarvis.tests/core.jarvis.tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName" --logger console --verbosity normal
```

## Frontend Commands (in core.jarvis.ui.studio/)
```bash
# Development server
npm run dev

# Build for production
npm run build

# Run linting
npm run lint

# Type checking
npm run typecheck

# Run unit tests
npm run test

# Run Playwright E2E tests
npm run test:e2e

# Run all tests
npm run test:all
```

## PowerShell Commands (for Windows environment)
```powershell
# Use when in WSL to run Windows commands
powershell.exe -Command "command here"
```

## Docker Commands
```bash
# Start PostgreSQL
docker-compose up -d

# Stop services
docker-compose down
```

## Git Commands
```bash
# Status
git status

# Add and commit
git add .
git commit -m "message"

# Push to remote
git push origin branch-name
```

## System Commands
```bash
# List files
ls -la

# Change directory
cd path/to/directory

# Find files
find . -name "*.cs"

# Search in files (use ripgrep)
rg "pattern" --type cs
```