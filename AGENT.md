# Jarvis Agent Guide

## Build/Test Commands
- **Build all:** `dotnet build`
- **Run all tests:** `dotnet test`
- **Run single test:** `dotnet test --filter "MethodName" --logger console --verbosity normal`
- **Test with coverage:** `dotnet test --collect:"XPlat Code Coverage"`
- **Build specific project:** `dotnet build core.jarvis/core.jarvis.csproj`

## Architecture
- **Entity Component System (ECS)** framework for .NET 8.0
- **Three core SDKs:** `core.jarvis` (ECS), `core.jarvis.data` (PostgreSQL), `core.jarvis.api` (Azure Functions)
- **Handler pattern:** Business logic in `ComponentHandler<T>` classes
- **JWT Row Level Security** with PostgreSQL database
- **MediatR** for command/query handling

## Projects Structure
- `core.jarvis/` - Main ECS framework
- `core.jarvis.data/` - Data access with Dapper/Npgsql
- `core.jarvis.api/` - Azure Functions REST API
- `*.tests/` - xUnit test projects with Shouldly assertions

## Code Style
- **Nullable enabled**, implicit usings, .NET 8.0, latest C# language version
- **PascalCase:** classes, methods, properties, interfaces (I-prefix)
- **camelCase:** parameters, private fields (_underscore prefix)
- **System imports first**, then third-party, then local namespaces
- **XML documentation** required for all public APIs
- **Guard clauses** for validation, **no Moq** (use test doubles)
