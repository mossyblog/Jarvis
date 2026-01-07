# Testing Guide

Jarvis testing patterns - from first test to complex business scenarios.

## Tests as Living Documentation

**Tests are not just verification tools—they are living documentation of intent.**

When code changes, tests may break. When tests break, developers need to understand:
1. What was the original intent?
2. Is the test outdated or is the code wrong?
3. What business requirement drove this test?

Without documented intent, developers make guesses. Guesses lead to bugs.

---

## Mandatory: Intent/Purpose Documentation

**Every test class and test method MUST have XML documentation with six fields:**

### Class-Level Documentation

```csharp
/// <summary>
/// INTENT: [What this test class is trying to verify]
/// PURPOSE: [Why these tests exist as a group]
/// BUSINESS CONTEXT: [What business requirement/user story this supports]
/// WHY IMPORTANT: [What breaks if these tests fail]
/// ARCHITECTURAL SIGNIFICANCE: [What system/layer/component this tests]
/// FUTURE RESILIENCE: [What regressions this prevents]
/// </summary>
public class RefreshTokenEndpointTests : ApiIntegrationTestBase
```

### Method-Level Documentation

```csharp
/// <summary>
/// INTENT: [What this specific test verifies]
/// PURPOSE: [Why this scenario matters]
/// BUSINESS CONTEXT: [User/business impact]
/// WHY IMPORTANT: [Security/functionality/UX implications]
/// ARCHITECTURAL SIGNIFICANCE: [Component/integration being tested]
/// FUTURE RESILIENCE: [What future bugs this catches]
/// </summary>
[Fact]
public async Task RefreshToken_WithValidToken_ReturnsNewAccessToken()
```

### Field Definitions

| Field | Question It Answers | Example |
|-------|---------------------|---------|
| **INTENT** | What are we testing? | "Verify valid refresh token returns new access token" |
| **PURPOSE** | Why does this test exist? | "Test the happy path for token refresh" |
| **BUSINESS CONTEXT** | What user need does this serve? | "Users with valid sessions should get new tokens" |
| **WHY IMPORTANT** | What breaks if this fails? | "Core functionality for session management" |
| **ARCHITECTURAL SIGNIFICANCE** | What system part is tested? | "Tests AuthSystem.RefreshToken integration" |
| **FUTURE RESILIENCE** | What regressions does this catch? | "Catches breaks in token rotation" |

### Why This Matters

```
6 months from now...

Developer: "This test is failing after my refactor. What was it even testing?"

WITHOUT INTENT: Developer guesses, potentially breaks production.

WITH INTENT: Developer reads "BUSINESS CONTEXT: Stolen refresh tokens
should not work indefinitely" and realizes their change broke a
security control, not just an arbitrary test.
```

---

## The No Mocks Policy

Jarvis enforces a strict NO MOCKS policy.

**Mocks test implementations, not behavior.** When you mock a dependency, you test whether your code calls the mock correctly - not whether it actually works.

**Mocks create false confidence.** Tests pass because mocks return what you told them to. The real system might behave differently.

**Mocks hide integration issues.** The most costly bugs occur at boundaries between components. Mocks eliminate those boundaries in tests.

Instead, Jarvis uses:
- Real PostgreSQL database (Docker-provisioned)
- Real DataContext with actual persistence
- Real handlers with actual business logic

---

## Directory Structure

```
core.jarvis.api.tests/
├── Integration/                    # Tests that hit real HTTP endpoints + database
│   ├── Auth/                       # Authentication endpoint tests
│   ├── Accounts/                   # Account endpoint tests
│   ├── Roles/                      # Role management endpoint tests
│   ├── GraphQL/                    # GraphQL endpoint tests
│   └── Handlers/                   # Handler integration tests
│
├── Unit/                           # Isolated component tests
│   ├── Processors/                 # Pre/post processor tests
│   ├── Services/                   # Service layer tests
│   └── Systems/                    # System orchestration tests
│
├── Handlers/                       # Handler-specific tests
├── Security/                       # Security-focused tests
│
└── Helpers/                        # Test infrastructure
    ├── ApiIntegrationTestBase.cs   # Base for API tests
    └── TestFactory.cs              # WebApplicationFactory setup

core.jarvis.tests/
├── Integration/                    # Core framework integration tests
├── Unit/                           # Core framework unit tests
└── Helpers/
    └── IntegrationTestBase.cs      # Base for all integration tests
```

---

## Test Base Classes

### IntegrationTestBase

Use for tests that need database access but NOT HTTP endpoints:

```csharp
/// <summary>
/// INTENT: Test OrderHandler create and status operations
/// PURPOSE: Verify order lifecycle management works correctly
/// BUSINESS CONTEXT: Orders are core business entities requiring reliable CRUD
/// WHY IMPORTANT: Order failures directly impact revenue
/// ARCHITECTURAL SIGNIFICANCE: Tests handler + component persistence layer
/// FUTURE RESILIENCE: Catches order state management regressions
/// </summary>
public class OrderHandlerTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify order creation with valid customer data
    /// PURPOSE: Test happy path for new order creation
    /// BUSINESS CONTEXT: Customers must be able to place orders
    /// WHY IMPORTANT: Core e-commerce functionality
    /// ARCHITECTURAL SIGNIFICANCE: Tests OrderHandler.CreateOrder integration
    /// FUTURE RESILIENCE: Catches order creation regressions
    /// </summary>
    [Fact]
    public async Task CanCreateOrder()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        // Act
        var order = await TestDataContext().For<OrderHandler>(entityId)
            .CreateOrder("Customer-123", 100.00m);

        // Assert
        order.CustomerId.ShouldBe("Customer-123");
        order.Status.ShouldBe("PENDING");
    }
}
```

**Provides:**
- `TestDataContext()` - Configured DataContext with real PostgreSQL
- `TrackEntity(Guid)` - Registers entities for automatic cleanup
- `Logger()` - Test-scoped logger for debugging

### ApiIntegrationTestBase

Use for tests that need HTTP endpoints AND database access:

```csharp
/// <summary>
/// INTENT: Test POST /security/refresh endpoint for token renewal
/// PURPOSE: Ensure refresh tokens can be exchanged for new access tokens
/// BUSINESS CONTEXT: Users need seamless session continuation without re-authentication
/// WHY IMPORTANT: Broken refresh flow forces users to re-login, degrading UX
/// ARCHITECTURAL SIGNIFICANCE: Tests the full token rotation security model
/// FUTURE RESILIENCE: Prevents regression in token lifecycle management
/// </summary>
public class RefreshTokenEndpointTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify valid refresh token returns new access token
    /// PURPOSE: Test the happy path for token refresh
    /// BUSINESS CONTEXT: Users with valid sessions should get new tokens
    /// WHY IMPORTANT: Core functionality for session management
    /// ARCHITECTURAL SIGNIFICANCE: Tests AuthSystem.RefreshToken integration
    /// FUTURE RESILIENCE: Catches breaks in token rotation
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewAccessToken()
    {
        // Arrange - Create user and authenticate
        var email = $"refresh_valid_{Guid.NewGuid()}@example.com";
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        // ... create account ...

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act - Use refresh token
        var response = await client.PostAsync("/api/security/refresh", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

**Provides (in addition to IntegrationTestBase):**
- `TokenService` - JWT token generation
- `Configuration` - Test configuration
- `JarvisApiWebApplicationFactory` - Real HTTP client factory

---

## Test Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
```csharp
RefreshToken_WithValidToken_ReturnsNewAccessToken()
RefreshToken_WithInvalidToken_Returns401()
CreateOrder_WithNegativeAmount_ThrowsValidationException()
PublishPost_WhenAlreadyPublished_ThrowsBusinessRuleException()
```

---

## AAA Pattern (Arrange-Act-Assert)

**All tests MUST follow the AAA pattern with clear comments:**

```csharp
[Fact]
public async Task BlogHandler_CanCreateBlogAndGeneratePost()
{
    // Arrange
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);

    // Act
    var blog = await TestDataContext().For<BlogComponentHandler>(entityId)
        .CreateBlog("My Blog", "A test blog");

    var post = await TestDataContext().For<BlogHandler>(entityId)
        .GeneratePost(new BlogPostGenerationRequest { Topic = "Testing" });

    // Assert
    blog.ShouldNotBeNull();
    blog.Name.ShouldBe("My Blog");
    post.Title.ShouldContain("Testing");
    post.Status.ShouldBe("draft");
}
```

---

## Entity Cleanup with TrackEntity

**Always track entities created during tests:**

```csharp
var accountId = Guid.NewGuid();
var roleId = Guid.NewGuid();

// Track ALL entities for cleanup (prevents test pollution)
TrackEntity(accountId);
TrackEntity(roleId);

await TestDataContext().Commit(account);
await TestDataContext().Commit(role);

// Tracked entities are automatically cleaned up after test
```

---

## Authentication in Tests

### Generating Test Tokens

```csharp
// Simple token
var token = TokenService.AccessToken(userId, email);

// Token with claims
var token = TokenService.AccessToken(userId, email, new Dictionary<string, string>
{
    { "roles", "admin" }
});
```

### Using Tokens in HTTP Requests

```csharp
await using var factory = new JarvisApiWebApplicationFactory();
using var client = factory.CreateClient();

client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync("/api/protected-endpoint");
```

### Creating Test Accounts

```csharp
var email = $"test_{Guid.NewGuid()}@example.com";  // Unique email
var password = "TestPassword123!";
var ownerEntityId = Guid.NewGuid();

TrackEntity(ownerEntityId);

var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
var hashedPassword = passwordService.HashPassword(password);

var account = new Account
{
    OwnerEntityId = ownerEntityId,
    Email = email,
    PasswordHash = hashedPassword,
    Password = "",  // Never store plain password
    AuthMethod = "password",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    LastUpdated = DateTime.UtcNow
};

await TestDataContext().Commit(account);
```

---

## Testing Validation Rules

Test that invalid input throws `ValidationException`:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void Guard_AgainstEmpty_ThrowsForInvalidStrings(string? value)
{
    var exception = Should.Throw<ValidationException>(() =>
        Guard.AgainstEmpty(value, "customerName"));

    exception.Errors.ShouldContainKey("customerName");
}
```

Test that valid input passes:

```csharp
[Theory]
[InlineData(0, 0, 100)]
[InlineData(50, 0, 100)]
[InlineData(100, 0, 100)]
public void Guard_AgainstOutOfRange_AllowsValidValues(int value, int min, int max)
{
    Should.NotThrow(() => Guard.AgainstOutOfRange(value, min, max, "value"));
}
```

---

## Testing Business Rule Violations

```csharp
/// <summary>
/// INTENT: Verify orders cannot be confirmed after payment
/// PURPOSE: Test business rule enforcement for order state transitions
/// BUSINESS CONTEXT: Paid orders are final - no changes allowed
/// WHY IMPORTANT: Prevents financial inconsistencies
/// ARCHITECTURAL SIGNIFICANCE: Tests state machine enforcement
/// FUTURE RESILIENCE: Catches invalid state transition bugs
/// </summary>
[Fact]
public async Task OrderHandler_ThrowsWhenConfirmingPaidOrder()
{
    // Arrange
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);

    var handler = TestDataContext().For<OrderHandler>(entityId);
    await handler.CreateOrder("customer", 100m);
    await handler.MarkAsPaid();

    // Act & Assert
    var ex = await Should.ThrowAsync<BusinessRuleException>(
        () => handler.ConfirmOrder());

    ex.Code.ShouldBe("ORDER_INVALID_STATE");
}
```

---

## Handling Expected Failures

When rate limiting or other factors may affect response:

```csharp
// Accept multiple valid statuses
var validStatuses = new[] { HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests };
validStatuses.ShouldContain(response.StatusCode,
    $"Expected 401 or 429 but got {response.StatusCode}");
```

---

## Shouldly Assertion Patterns

```csharp
// Null checks
result.ShouldNotBeNull();

// Equality
result.Name.ShouldBe("Expected");
result.Id.ShouldNotBe(Guid.Empty);

// Collections
result.Items.ShouldNotBeEmpty();
result.Permissions.ShouldContain("admin.read");

// Strings
result.Email.ShouldNotBeNullOrEmpty();
result.Hash.ShouldStartWith("$2");

// Comparisons
result.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue);
```

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test core.jarvis.tests

# Run tests matching filter
dotnet test --filter "FullyQualifiedName~BlogHandler"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

---

## CRITICAL: Mandatory Account Creation for Authenticated Tests

**Every test that uses JWT authentication MUST create a real account in the database.**

The `UserValidationPreProcessor` validates that the user ID in JWT claims exists in the database. Tests that create JWT tokens for non-existent users ("phantom users") will fail with 401 Unauthorized.

### The Phantom User Anti-Pattern (BANNED)

```csharp
// WRONG: Creates a token for a user that doesn't exist in the database
[Fact]
public async Task SomeEndpoint_WithToken_Works()
{
    var phantomUserId = Guid.NewGuid();  // This user doesn't exist!
    var token = TokenService.AccessToken(phantomUserId, "phantom@example.com");

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await client.GetAsync("/api/protected");  // FAILS: 401
}
```

### The Correct Pattern (REQUIRED)

```csharp
// CORRECT: Creates a real account before generating a token
[Fact]
public async Task SomeEndpoint_WithValidUser_Works()
{
    // Arrange - Create a REAL account
    var email = $"test_{Guid.NewGuid()}@example.com";
    var account = await CreateTestAccount(email, "TestPassword123!");
    TrackEntity(account.OwnerEntityId);  // Track for cleanup

    // Create security profile (required for most operations)
    var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
    await profileHandler.CreateWithDefaults(email);

    // Generate token for the REAL user
    var token = TokenService.AccessToken(account.OwnerEntityId, email);

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await client.GetAsync("/api/protected");  // Works!
}

// Helper method (add to your test class)
private async Task<Account> CreateTestAccount(string email, string password)
{
    var entityId = Guid.NewGuid();
    var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
    var hashedPassword = passwordService.HashPassword(password);

    var account = new Account
    {
        OwnerEntityId = entityId,
        Email = email,
        PasswordHash = hashedPassword,
        Password = "",
        AuthMethod = "password",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        LastUpdated = DateTime.UtcNow
    };
    await TestDataContext().Commit(account);
    return account;
}
```

### Why This Matters

The `UserValidationPreProcessor` closes a critical security gap:

1. **Before**: JWT tokens were validated cryptographically but not against the database
2. **Problem**: Attackers could create valid JWTs for users that don't exist
3. **Risk**: RLS policies would receive fake user IDs, potentially bypassing security
4. **Solution**: Every authenticated request now verifies the user exists and is active

**Tests that skip account creation are simulating an attack vector, not real user behavior.**

---

## Anti-Patterns to Avoid

| Anti-Pattern | Problem | Solution |
|--------------|---------|----------|
| Missing intent docs | Future devs don't know WHY | Add all 6 documentation fields |
| Not tracking entities | Test pollution | Use `TrackEntity()` |
| Hardcoded test data | Test conflicts | Use `$"test_{Guid.NewGuid()}@..."` |
| Using mocks | False confidence | Use real services |
| Testing implementation | Brittle tests | Test observable behavior |
| **Phantom users in JWT** | **Security hole, tests fail** | **Create real accounts first** |

---

## Checklist for New Tests

- [ ] Class has full XML documentation (all 6 fields)
- [ ] Each test method has full XML documentation (all 6 fields)
- [ ] Test name follows `MethodName_Scenario_ExpectedResult` convention
- [ ] Test follows AAA pattern with clear comments
- [ ] All created entities are tracked with `TrackEntity()`
- [ ] Test data uses unique identifiers
- [ ] No mocks used
- [ ] Shouldly assertions used
- [ ] **Authenticated tests create REAL accounts in the database (no phantom users)**

---

**Next:** [06-database.md](06-database.md) - Database and data access
