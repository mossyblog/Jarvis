# Jarvis

A modern Entity Component System (ECS) framework for .NET 8 that provides flexible data orchestration with a plugin-based handler architecture. The framework consists of three complementary SDKs designed for different levels of data access and orchestration.

## Quick Start

```bash
# Clone the repository
git clone https://github.com/yourusername/jarvis.git
cd jarvis

# Start PostgreSQL with Docker
docker-compose up -d

# Build the solution
dotnet build

# Run tests
dotnet test

# Start the API (optional)
cd core.jarvis.api
func start
```

## Features

- **Plugin-Based Handler Architecture**: Modular business logic encapsulation through handlers
- **Entity Component System**: Clean separation of data (components) from logic (handlers)
- **Triple SDK Architecture**: 
  - `core.jarvis` - High-level ECS orchestration with handler pattern
  - `core.jarvis.data` - Low-level PostgreSQL data access with JWT-based security
  - `core.jarvis.api` - Azure Functions API layer for authentication endpoints
- **Type-Safe Query System**: Fluent API for cross-component entity queries
- **Built-in Audit Trail**: Comprehensive event sourcing and audit capabilities
- **Row Level Security**: SDK-enforced access control with JWT authentication
- **Transaction Support**: ACID-compliant operations through `InTransaction()`
- **Supabase Integration**: First-class support for Supabase PostgreSQL backend

## Architecture Overview

### core.jarvis - ECS Orchestration Framework

The main framework provides high-level abstractions for entity-component management:

- **IDataContext**: Main entry point for handler resolution and transactions
- **IComponentHandler**: Base interface for all business logic handlers
- **IEntityQuery**: Type-safe, reflection-free entity querying
- **Plugin Extensions**: Clean extension methods for handler access

### core.jarvis.data - Data Access SDK

Low-level PostgreSQL data access with security features:

- **PgClient**: JWT-authenticated PostgreSQL client
- **PgTable<T>**: Type-safe table operations with automatic PascalCase to snake_case mapping
- **RLS Policies**: SDK-enforced Row Level Security
- **Direct SQL Access**: When you need raw performance

### core.jarvis.api - API Layer

Azure Functions-based REST API for authentication and security:

- **Authentication Endpoints**: Login, logout, refresh tokens, and validation
- **JWT Token Management**: Secure token generation and validation
- **Swagger/OpenAPI**: Auto-generated API documentation
- **Component Validation**: Middleware for validating component payloads

## Installation

### Install the SDKs:

```bash
# ECS Framework
dotnet add reference path/to/core.jarvis/core.jarvis.csproj

# Data Access SDK
dotnet add reference path/to/core.jarvis.data/core.jarvis.data.csproj

# API Layer (optional)
dotnet add reference path/to/core.jarvis.api/core.jarvis.api.csproj
```

Or via NuGet (when published):

```bash
dotnet add package core.jarvis
dotnet add package core.jarvis.data
dotnet add package core.jarvis.api
```

## Configuration

### ECS Framework Setup

```csharp
// In your Startup.cs or Program.cs
services.AddJarvis(options =>
{
    options.SupabaseUrl = "https://your-project.supabase.co";
    options.SupabaseKey = "your-anon-key";
    options.ServiceRoleKey = "your-service-role-key"; // For admin operations
});

// Register your handlers
services.AddScoped<IComponentHandler, InvoiceHandler>();
services.AddScoped<IComponentHandler, PaymentHandler>();
```

### Data Access SDK Setup

```csharp
// Create a client factory
var factory = new PgClientFactory(
    "https://your-project.supabase.co",
    "your-anon-key"
);

// Create authenticated client
var client = factory.Create();
var jwt = await client.Authenticate("user@example.com", "password");
client.JWT(jwt);
```

### API Layer Setup (Azure Functions)

Configure in `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Jwt:Secret": "your-secret-key-min-32-chars",
    "Jwt:Issuer": "jarvis-api",
    "Jwt:Audience": "jarvis-client",
    "Jwt:ExpirationMinutes": "15"
  },
  "ConnectionStrings": {
    "JarvisDb": "Host=localhost;Port=5432;Database=jarvis;Username=postgres;Password=postgres"
  }
}
```

Run the API locally:

```bash
cd core.jarvis.api
func start

# API endpoints available at:
# http://localhost:7071/api/security/auth
# http://localhost:7071/api/swagger/ui
```

## Usage Examples

### Handler-Based Operations (Recommended)

```csharp
// Resolve handler and perform operations
var invoiceHandler = dataContext.For<InvoiceHandler>(entityId);
var invoice = await invoiceHandler.GetAsync();
await invoiceHandler.UpdateStatusAsync("paid");

// With plugin extensions
var invoice = await dataContext.Invoice(entityId).GetAsync();
```

### Entity Queries

```csharp
// Find all entities with both Invoice and Payment components
var paidInvoices = await dataContext.Query()
    .WithAll<Invoice, Payment>()
    .ToListAsync();

// Find entities with Invoice but no Payment
var unpaidInvoices = await dataContext.Query()
    .WithAll<Invoice>()
    .WithNone<Payment>()
    .ToListAsync();
```

### Transaction Support

```csharp
var result = await dataContext.InTransaction(async transaction =>
{
    var invoice = await transaction.Invoice(invoiceId).GetAsync();
    await transaction.Payment(paymentId).ProcessAsync();
    await transaction.Invoice(invoiceId).MarkPaidAsync();
    return invoice;
});
```

### Direct Data Access

```csharp
// When you need raw SQL performance
var products = await client.From<Product>()
    .Filter("price", "gte", 100)
    .Filter("category", "eq", "electronics")
    .Order("created_at", "desc")
    .Limit(10)
    .Get();
```

## Component Definition

Components must be defined as records for immutability:

```csharp
public record Invoice : BaseComponent
{
    public string InvoiceNumber { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; }
    public DateTime DueDate { get; init; }
}
```

## Handler Implementation

```csharp
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public InvoiceHandler(IDataContext dataContext, IAuditService auditService) 
        : base(dataContext, auditService)
    {
    }

    public async Task<Invoice> CreateAsync(decimal amount, DateTime dueDate)
    {
        var invoice = new Invoice
        {
            EntityId = Guid.NewGuid(),
            InvoiceNumber = GenerateInvoiceNumber(),
            Amount = amount,
            Status = "pending",
            DueDate = dueDate
        };

        await SaveAsync(invoice);
        await AuditAsync("invoice_created", invoice);
        
        return invoice;
    }
}
```

## Database Schema

Components are stored in PostgreSQL tables with snake_case naming:

```sql
-- Components table (one per component type)
CREATE TABLE invoice (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id UUID NOT NULL UNIQUE,
    invoice_number TEXT NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    status TEXT NOT NULL,
    due_date TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Audit events table
CREATE TABLE audit_event (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id UUID NOT NULL,
    event_type TEXT NOT NULL,
    event_data JSONB,
    user_id UUID,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

## Testing

The framework includes comprehensive testing utilities:

```csharp
public class InvoiceHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateInvoice_ShouldPersistWithCorrectData()
    {
        // Arrange
        var handler = TestDataContext().For<InvoiceHandler>(Guid.NewGuid());
        
        // Act
        var invoice = await handler.CreateAsync(100.50m, DateTime.UtcNow.AddDays(30));
        
        // Assert
        invoice.Amount.ShouldBe(100.50m);
        invoice.Status.ShouldBe("pending");
    }
}
```

### Running Tests Locally

```bash
# Run all tests
dotnet test

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Database Setup

Integration tests require a PostgreSQL database. You can use either:

1. **Docker Compose** (Recommended):
```bash
# Start Supabase PostgreSQL container
docker-compose up -d

# Database will be available at localhost:5432
# Default credentials: postgres/postgres
```

2. **Manual PostgreSQL Setup**:
```bash
# Create test database
createdb jarvis_test

# Run setup script
psql -d jarvis_test -f core.jarvis.tests/Scripts/setup-test-database.sql
```

Set the connection string via environment variable:
```bash
export TEST_DATABASE_URL="Host=localhost;Port=5432;Database=jarvis_test;Username=postgres;Password=postgres"
```

## CI/CD with GitHub Actions

The project uses GitHub Actions for continuous integration with full database integration tests:

### Build Status
[![.NET](https://github.com/yourusername/jarvis/actions/workflows/dotnet.yml/badge.svg)](https://github.com/yourusername/jarvis/actions/workflows/dotnet.yml)

### CI Pipeline Features

- **Automated Testing**: All tests run on every push and pull request
- **Real Database Tests**: Uses Supabase PostgreSQL container (supabase/postgres:15.1.0.155)
- **Database Initialization**: Automatically sets up test schema and data
- **Code Coverage**: Reports coverage metrics to Codecov
- **No Mocks**: All integration tests use real database connections

### GitHub Actions Workflow

The CI pipeline automatically:
1. Spins up a Supabase PostgreSQL container
2. Initializes the database with test schema
3. Runs all unit and integration tests
4. Reports test results and code coverage

See [.github/workflows/dotnet.yml](.github/workflows/dotnet.yml) for the complete workflow configuration.

## Documentation

For detailed documentation:

- [Getting Started Guide](docs/getting-started/getting-started-readme.md)
- [Architecture Overview](docs/architecture/architecture-readme.md)
- [Handler Development](docs/guides/handler-development.md)
- [Testing Strategies](docs/guides/testing-strategies.md)
- [Migration Guide](docs/migration/migration-readme.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.