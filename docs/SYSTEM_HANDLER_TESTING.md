# System + Handler Testing Guide

## Table of Contents
1. [Testing Philosophy](#testing-philosophy)
2. [Testing Systems](#testing-systems)
3. [Testing Handlers](#testing-handlers)
4. [Testing Azure Functions](#testing-azure-functions)
5. [Integration Testing](#integration-testing)
6. [Common Testing Patterns](#common-testing-patterns)
7. [Anti-Patterns to Avoid](#anti-patterns-to-avoid)

## Testing Philosophy

The System + Handler architecture enables focused, isolated testing at each layer:
- **Systems**: Test orchestration logic and business rule enforcement
- **Handlers**: Test component CRUD operations
- **Functions**: Test HTTP adaptation only
- **Integration**: Test complete workflows end-to-end

### Core Testing Principles
1. **Test behavior, not implementation**
2. **Use real dependencies when possible** (no mocks)
3. **Each test should be independent**
4. **Tests should be deterministic**
5. **Follow AAA pattern** (Arrange, Act, Assert)

## Testing Systems

Systems contain the most complex business logic and require thorough testing.

### Basic System Test Structure
```csharp
public class RegistrationSystemTests : IntegrationTestBase
{
    private RegistrationSystem GetSystem() => 
        _serviceProvider.GetRequiredService<RegistrationSystem>();

    /// <summary>
    /// INTENT: Verify successful user registration creates Account and SecurityProfile
    /// PURPOSE: Ensure the happy path works correctly
    /// BUSINESS CONTEXT: New users must be able to register
    /// WHY IMPORTANT: Registration is the entry point for all users
    /// ARCHITECTURAL SIGNIFICANCE: Tests system orchestration of multiple handlers
    /// FUTURE RESILIENCE: Ensures registration workflow remains intact
    /// </summary>
    [Fact]
    public async Task RegisterUser_WithValidData_CreatesAccountAndProfile()
    {
        // Arrange
        var requestJson = """
        {
            "email": "newuser@test.com",
            "password": "ValidPass123!",
            "fullName": "Test User"
        }
        """;
        
        // Act
        var components = await GetSystem().RegisterUser(requestJson, "127.0.0.1");
        
        // Assert
        components.Count.ShouldBe(2);
        
        var account = components.OfType<Account>().FirstOrDefault();
        var profile = components.OfType<SecurityProfile>().FirstOrDefault();
        
        account.ShouldNotBeNull();
        account.Email.ShouldBe("newuser@test.com");
        account.IsActive.ShouldBeTrue();
        account.PasswordHash.ShouldNotBeEmpty();
        account.Password.ShouldBeEmpty(); // Never store plain password
        
        profile.ShouldNotBeNull();
        profile.Name.ShouldBe("Test User");
        profile.OwnerEntityId.ShouldBe(account.OwnerEntityId);
        
        // Track for cleanup
        TrackEntity(account.OwnerEntityId);
    }
}
```

### Testing System Validation
```csharp
[Fact]
public async Task RegisterUser_WithExistingEmail_ThrowsBusinessRuleException()
{
    // Arrange - Create existing user
    var existingAccount = new Account
    {
        Id = Guid.NewGuid(),
        OwnerEntityId = Guid.NewGuid(),
        Email = "existing@test.com",
        PasswordHash = "hash",
        Password = "",
        AuthMethod = "password",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    await TestDataContext().Commit(existingAccount);
    TrackEntity(existingAccount.OwnerEntityId);
    
    var requestJson = """
    {
        "email": "existing@test.com",
        "password": "ValidPass123!"
    }
    """;
    
    // Act & Assert
    var exception = await Should.ThrowAsync<BusinessRuleException>(
        async () => await GetSystem().RegisterUser(requestJson, "127.0.0.1"));
    
    exception.Code.ShouldBe("RULE_EMAIL_EXISTS");
    exception.Message.ShouldBe("EMAIL_EXISTS");
}
```

### Testing Complex Orchestration
```csharp
[Fact]
public async Task InvoiceSystem_CreateWithLineItems_CalculatesTotalCorrectly()
{
    // Arrange
    var customerId = Guid.NewGuid();
    TrackEntity(customerId);
    
    var request = new InvoiceCreationRequest
    {
        CustomerId = customerId,
        PaymentTermDays = 30,
        LineItems = new[]
        {
            new LineItemRequest { Description = "Service A", Quantity = 2, UnitPrice = 100.00m },
            new LineItemRequest { Description = "Service B", Quantity = 1, UnitPrice = 50.00m }
        }
    };
    
    var system = _serviceProvider.GetRequiredService<InvoiceSystem>();
    
    // Act
    var components = await system.CreateInvoiceWithLineItems(request);
    
    // Assert
    var invoice = components.OfType<Invoice>().First();
    var lineItems = components.OfType<InvoiceLineItem>().ToList();
    
    invoice.TotalAmount.ShouldBe(250.00m); // (2 * 100) + (1 * 50)
    invoice.Status.ShouldBe(InvoiceStatus.Draft);
    invoice.DueDate.ShouldBeGreaterThan(DateTime.UtcNow.AddDays(29));
    
    lineItems.Count.ShouldBe(2);
    lineItems.All(item => item.InvoiceId == invoice.Id).ShouldBeTrue();
}
```

## Testing Handlers

Handler tests focus on component CRUD operations and component-specific business rules.

### Basic Handler Test
```csharp
public class AccountHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateAccount_WithValidData_StoresAccount()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        var handler = TestDataContext().For<AccountHandler>(entityId);
        var newAccount = new Account
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed",
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Act
        var account = await handler.CreateAccount(newAccount);
        
        // Assert
        account.OwnerEntityId.ShouldBe(entityId);
        account.Email.ShouldBe("test@example.com");
        
        // Verify it was persisted
        var retrieved = await handler.Get();
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(account.Id);
    }
}
```

### Testing Handler Updates
```csharp
[Fact]
public async Task AccountHandler_Deactivate_UpdatesIsActiveFlag()
{
    // Arrange
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);
    
    var handler = TestDataContext().For<AccountHandler>(entityId);
    var account = await handler.CreateAccount(new Account
    {
        Id = Guid.NewGuid(),
        Email = "active@test.com",
        PasswordHash = "hash",
        Password = "",
        AuthMethod = "password",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    });
    
    // Act
    var deactivated = await handler.Deactivate();
    
    // Assert
    deactivated.IsActive.ShouldBeFalse();
    deactivated.UpdatedAt.ShouldBeGreaterThan(account.UpdatedAt);
    deactivated.Email.ShouldBe(account.Email); // Other fields unchanged
}
```

### Testing Handler Queries
```csharp
[Fact]
public async Task ProfileHandler_Get_ReturnsNullWhenNotExists()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var handler = TestDataContext().For<AccountProfileHandler>(entityId);
    
    // Act
    var profile = await handler.Get();
    
    // Assert
    profile.ShouldBeNull();
}

[Fact]
public async Task ProfileHandler_CreateWithDefaults_SetsDefaultValues()
{
    // Arrange
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);
    
    var handler = TestDataContext().For<AccountProfileHandler>(entityId);
    
    // Act
    var profile = await handler.CreateWithDefaults("user@test.com");
    
    // Assert
    profile.Name.ShouldBe("user"); // Email prefix as default
    profile.RoleIds.ShouldNotBeEmpty(); // Should have default role
    profile.PermissionIds.ShouldBeEmpty();
}
```

## Testing Azure Functions

Function tests should be minimal, focusing only on HTTP concerns.

### Basic Function Test
```csharp
public class RegisterFunctionTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreatedStatus()
    {
        // Arrange
        var function = new RegisterFunction(
            _serviceProvider.GetRequiredService<RegistrationSystem>(),
            _serviceProvider.GetRequiredService<ILogger<RegisterFunction>>());
        
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "POST";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""
        {
            "email": "functest@test.com",
            "password": "ValidPass123!"
        }
        """));
        
        var httpRequest = new DefaultHttpRequestData(context);
        
        // Act
        var response = await function.Register(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        // Verify response body contains components
        var responseBody = await ReadResponseBody(response);
        responseBody.ShouldContain("functest@test.com");
        
        // Track for cleanup
        var components = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(responseBody);
        var accountData = components.First(c => c.ContainsKey("email"));
        TrackEntity(Guid.Parse(accountData["ownerEntityId"].ToString()));
    }
}
```

### Testing Function Error Handling
```csharp
[Fact]
public async Task Register_WithInvalidJson_ReturnsBadRequest()
{
    // Arrange
    var function = new RegisterFunction(
        _serviceProvider.GetRequiredService<RegistrationSystem>(),
        _serviceProvider.GetRequiredService<ILogger<RegisterFunction>>());
    
    var httpRequest = CreateHttpRequest("{ invalid json }");
    
    // Act
    var response = await function.Register(httpRequest);
    
    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    
    var responseBody = await ReadResponseBody(response);
    var error = JsonSerializer.Deserialize<ValidationProblemDetails>(responseBody);
    error.Errors.ShouldContainKey("body");
}
```

## Integration Testing

Integration tests verify complete workflows from HTTP request to database persistence.

### Full Registration Flow Test
```csharp
[Fact]
public async Task RegistrationFlow_CompleteScenario_WorksEndToEnd()
{
    // Arrange
    var email = $"integration-{Guid.NewGuid()}@test.com";
    var requestJson = $$"""
    {
        "email": "{{email}}",
        "password": "SecurePass123!",
        "fullName": "Integration Test User"
    }
    """;
    
    var registrationSystem = _serviceProvider.GetRequiredService<RegistrationSystem>();
    var authHandler = TestDataContext().For<AuthHandler>(Guid.Empty);
    
    // Act 1: Register user
    var components = await registrationSystem.RegisterUser(requestJson, "127.0.0.1");
    
    // Assert 1: Registration successful
    var account = components.OfType<Account>().First();
    var profile = components.OfType<SecurityProfile>().First();
    
    account.Email.ShouldBe(email);
    profile.Name.ShouldBe("Integration Test User");
    
    TrackEntity(account.OwnerEntityId);
    
    // Act 2: Try to login
    var authToken = await authHandler.AuthenticateFromJson($$"""
    {
        "email": "{{email}}",
        "password": "SecurePass123!"
    }
    """, "127.0.0.1", "TestAgent");
    
    // Assert 2: Login successful
    authToken.ShouldNotBeNull();
    authToken.AccountId.ShouldBe(account.OwnerEntityId);
    authToken.AccessToken.ShouldNotBeEmpty();
    
    // Act 3: Verify profile access
    var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
    var retrievedProfile = await profileHandler.Get();
    
    // Assert 3: Profile accessible
    retrievedProfile.ShouldNotBeNull();
    retrievedProfile.Name.ShouldBe("Integration Test User");
}
```

### Testing Transaction Rollback (When Available)
```csharp
[Fact]
public async Task InvoiceCreation_WithError_RollsBackAllChanges()
{
    // Arrange
    var system = _serviceProvider.GetRequiredService<InvoiceSystem>();
    var request = new InvoiceCreationRequest
    {
        CustomerId = Guid.NewGuid(),
        LineItems = new[]
        {
            new LineItemRequest { Description = "Valid Item", Quantity = 1, UnitPrice = 100 },
            new LineItemRequest { Description = null!, Quantity = 0, UnitPrice = -50 } // Invalid
        }
    };
    
    // Act & Assert
    await Should.ThrowAsync<ValidationException>(
        async () => await system.CreateInvoiceWithLineItems(request));
    
    // Verify nothing was created
    var invoices = await TestDataContext().Query()
        .WithAll<Invoice>(i => i.CustomerId == request.CustomerId)
        .ToList<Invoice>();
    
    invoices.ShouldBeEmpty();
}
```

## Common Testing Patterns

### Pattern 1: Test Data Builders
```csharp
public class TestDataBuilder
{
    public static Account CreateTestAccount(string? email = null)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"test-{Guid.NewGuid()}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public static Invoice CreateTestInvoice(Guid customerId, decimal amount = 100m)
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{DateTime.UtcNow.Ticks}",
            CustomerId = customerId,
            TotalAmount = amount,
            Status = InvoiceStatus.Draft,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

// Usage in tests
[Fact]
public async Task SomeTest()
{
    var account = TestDataBuilder.CreateTestAccount("specific@test.com");
    // ...
}
```

### Pattern 2: Assertion Helpers
```csharp
public static class ComponentAssertions
{
    public static void ShouldBeValidAccount(this Account account, string expectedEmail)
    {
        account.ShouldNotBeNull();
        account.Id.ShouldNotBe(Guid.Empty);
        account.OwnerEntityId.ShouldNotBe(Guid.Empty);
        account.Email.ShouldBe(expectedEmail);
        account.PasswordHash.ShouldNotBeEmpty();
        account.Password.ShouldBeEmpty();
        account.CreatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        account.UpdatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }
    
    public static void ShouldContainComponent<T>(this List<IComponent> components) 
        where T : IComponent
    {
        components.OfType<T>().Any().ShouldBeTrue($"Expected to find {typeof(T).Name} in components");
    }
}

// Usage
[Fact]
public async Task TestWithHelpers()
{
    var components = await system.RegisterUser(json, ip);
    
    var account = components.OfType<Account>().First();
    account.ShouldBeValidAccount("test@example.com");
    
    components.ShouldContainComponent<SecurityProfile>();
}
```

### Pattern 3: Scenario Testing
```csharp
[Fact]
public async Task CompleteInvoiceScenario()
{
    // Setup
    var customerId = Guid.NewGuid();
    TrackEntity(customerId);
    
    var invoiceSystem = _serviceProvider.GetRequiredService<InvoiceSystem>();
    
    // Scenario: Create, Update, Finalize Invoice
    
    // Step 1: Create draft invoice
    var createRequest = new InvoiceCreationRequest
    {
        CustomerId = customerId,
        LineItems = new[] 
        { 
            new LineItemRequest { Description = "Consulting", Quantity = 10, UnitPrice = 150 }
        }
    };
    
    var components = await invoiceSystem.CreateInvoiceWithLineItems(createRequest);
    var invoice = components.OfType<Invoice>().First();
    
    invoice.Status.ShouldBe(InvoiceStatus.Draft);
    invoice.TotalAmount.ShouldBe(1500m);
    
    // Step 2: Add another line item
    var addItemComponents = await invoiceSystem.AddLineItem(invoice.Id, 
        new LineItemRequest { Description = "Support", Quantity = 5, UnitPrice = 100 });
    
    var updatedInvoice = addItemComponents.OfType<Invoice>().First();
    updatedInvoice.TotalAmount.ShouldBe(2000m); // 1500 + 500
    
    // Step 3: Finalize and send
    var finalizeComponents = await invoiceSystem.FinalizeAndSendInvoice(invoice.Id);
    var finalInvoice = finalizeComponents.OfType<Invoice>().First();
    var document = finalizeComponents.OfType<Document>().First();
    
    finalInvoice.Status.ShouldBe(InvoiceStatus.Sent);
    finalInvoice.SentAt.ShouldNotBeNull();
    document.FileName.ShouldContain(invoice.InvoiceNumber);
}
```

## Anti-Patterns to Avoid

### ❌ Don't Mock DataContext or Handlers
```csharp
// BAD: Mocking infrastructure
[Fact]
public async Task BadTest_WithMocks()
{
    var mockDataContext = new Mock<IDataContext>();
    var mockHandler = new Mock<AccountHandler>();
    
    mockDataContext.Setup(x => x.For<AccountHandler>(It.IsAny<Guid>()))
        .Returns(mockHandler.Object);
    
    // This tests mocks, not actual behavior!
}

// GOOD: Use real implementations
[Fact]
public async Task GoodTest_WithRealDependencies()
{
    var handler = TestDataContext().For<AccountHandler>(entityId);
    var account = await handler.CreateAccount(newAccount);
    
    // Tests actual behavior
}
```

### ❌ Don't Test Implementation Details
```csharp
// BAD: Testing private methods or internal state
[Fact]
public async Task BadTest_ChecksPrivateMethod()
{
    var system = new RegistrationSystem(/* ... */);
    
    // Using reflection to test private method
    var method = system.GetType().GetMethod("ValidateEmail", BindingFlags.NonPublic);
    var result = method.Invoke(system, new[] { "test@test.com" });
    
    // Don't do this!
}

// GOOD: Test through public interface
[Fact]
public async Task GoodTest_ValidatesEmailThroughPublicApi()
{
    var system = GetSystem();
    
    var exception = await Should.ThrowAsync<ValidationException>(
        async () => await system.RegisterUser("""{"email": "invalid"}""", null));
    
    exception.Errors["email"].ShouldContain("Invalid email format");
}
```

### ❌ Don't Create Test-Only Methods
```csharp
// BAD: Adding methods just for testing
public class AccountHandler : ComponentHandler<Account>
{
    // Don't add this!
    public async Task<bool> TestOnlyCheckEmailExists(string email)
    {
        return await DataContext.Query()
            .WithAll<Account>(a => a.Email == email)
            .Any();
    }
}

// GOOD: Test through actual use cases
[Fact]
public async Task GoodTest_ChecksEmailThroughRegistration()
{
    // Create account with email
    await TestDataContext().Commit(existingAccount);
    
    // Try to register with same email
    var exception = await Should.ThrowAsync<BusinessRuleException>(
        async () => await system.RegisterUser(jsonWithSameEmail, null));
    
    exception.Code.ShouldBe("RULE_EMAIL_EXISTS");
}
```

### ❌ Don't Skip Cleanup
```csharp
// BAD: Leaving test data in database
[Fact]
public async Task BadTest_NoCleanup()
{
    var account = new Account { /* ... */ };
    await TestDataContext().Commit(account);
    
    // No cleanup - will pollute database!
}

// GOOD: Always track entities for cleanup
[Fact]
public async Task GoodTest_WithCleanup()
{
    var entityId = Guid.NewGuid();
    TrackEntity(entityId); // Automatic cleanup
    
    var account = new Account { OwnerEntityId = entityId, /* ... */ };
    await TestDataContext().Commit(account);
    
    // Entity will be cleaned up automatically
}
```

## Summary

Testing in the System + Handler architecture follows these key principles:

1. **Test at the right level** - Systems for orchestration, Handlers for CRUD, Functions for HTTP
2. **Use real dependencies** - Avoid mocks, use actual database
3. **Test behavior, not implementation** - Focus on what, not how
4. **Clean up after tests** - Use TrackEntity for automatic cleanup
5. **Document test intent** - Use XML comments to explain why

This approach ensures tests are:
- **Reliable** - They test real behavior
- **Maintainable** - They don't break with refactoring
- **Valuable** - They catch actual bugs
- **Fast** - They run quickly with proper setup