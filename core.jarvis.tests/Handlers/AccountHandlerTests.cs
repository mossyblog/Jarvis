using core.jarvis.api.Extensions;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using core.jarvis.Data;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace core.jarvis.tests.Handlers;

/// <summary>
/// Integration tests for AccountHandler component lifecycle operations.
/// Tests verify proper account activation, deactivation, and registration workflows.
/// These tests run against a real database to ensure data persistence and context behavior.
/// </summary>
public class AccountHandlerTests : IntegrationTestBase
{
    private ServiceProvider? _apiServiceProvider;
    private IDataContext? _apiDataContext;

    /// <summary>
    /// Gets the data context configured with API services.
    /// </summary>
    protected IDataContext ApiDataContext => _apiDataContext
        ?? throw new InvalidOperationException("API services not initialized");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        InitializeApiServices();
    }

    private void InitializeApiServices()
    {
        var testConnectionString = TestDatabaseSetup.GetConnectionString();

        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:JarvisDb"] = testConnectionString,
                ["Jwt:Issuer"] = "jarvis-api-test",
                ["Jwt:Audience"] = "jarvis-test-clients",
                ["Jwt:SecretKey"] = "TEST_JARVIS_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_MINIMUM_256_BITS_LONG_TO_MEET_ALL_REQUIREMENTS",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            });

        var configuration = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddJarvisApiServices();

        _apiServiceProvider = services.BuildServiceProvider();
        _apiDataContext = _apiServiceProvider.GetRequiredService<IDataContext>();
    }

    public override async Task DisposeAsync()
    {
        _apiServiceProvider?.Dispose();
        await base.DisposeAsync();
    }

    /// <summary>
    /// INTENT: Verify that Activate successfully sets IsActive to true on an inactive account.
    /// PURPOSE: Ensures the account activation workflow correctly updates the account state.
    /// BUSINESS CONTEXT: Account activation is required after registration to enable user access.
    /// WHY IMPORTANT: Inactive accounts must be activatable to allow users to log in.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the handler's ability to use TryCommit with immutable records.
    /// FUTURE RESILIENCE: Validates activation continues to work after schema or handler changes.
    /// </summary>
    [Fact]
    public async Task Activate_WithInactiveAccount_SetsIsActiveToTrue()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        // Create an inactive account
        var inactiveAccount = new Account
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = ownerEntityId,
            Email = $"test_{Guid.NewGuid()}@example.com",
            PasswordHash = "hashed_password",
            Password = "",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await ApiDataContext.Commit(inactiveAccount);

        var timestampBefore = inactiveAccount.LastUpdated;

        // Wait to ensure timestamp difference
        await Task.Delay(10);

        var accountHandler = ApiDataContext.For<AccountHandler>(ownerEntityId);

        // Act
        var result = await accountHandler.Activate();

        // Assert - verify account was activated
        result.ShouldNotBeNull("Activate should return the updated account");
        result.IsActive.ShouldBeTrue("Account should be active after activation");
        result.OwnerEntityId.ShouldBe(ownerEntityId, "Owner entity ID should be preserved");
        result.Email.ShouldBe(inactiveAccount.Email, "Email should be preserved");
        result.LastUpdated.ShouldBeGreaterThan(timestampBefore, "LastUpdated should be newer after activation");

        // Cleanup
        await ApiDataContext.Remove<Account>(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Verify that Deactivate successfully sets IsActive to false on an active account.
    /// PURPOSE: Ensures the account deactivation workflow correctly updates the account state.
    /// BUSINESS CONTEXT: Account deactivation is used to disable user access without deleting the account.
    /// WHY IMPORTANT: Deactivated accounts should not be able to authenticate.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the handler's ability to toggle state on immutable records.
    /// FUTURE RESILIENCE: Validates deactivation continues to work after handler changes.
    /// </summary>
    [Fact]
    public async Task Deactivate_WithActiveAccount_SetsIsActiveToFalse()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        // Create an active account
        var activeAccount = new Account
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = ownerEntityId,
            Email = $"test_{Guid.NewGuid()}@example.com",
            PasswordHash = "hashed_password",
            Password = "",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await ApiDataContext.Commit(activeAccount);

        var timestampBefore = activeAccount.LastUpdated;

        // Wait to ensure timestamp difference
        await Task.Delay(10);

        var accountHandler = ApiDataContext.For<AccountHandler>(ownerEntityId);

        // Act
        var result = await accountHandler.Deactivate();

        // Assert - verify account was deactivated
        result.ShouldNotBeNull("Deactivate should return the updated account");
        result.IsActive.ShouldBeFalse("Account should be inactive after deactivation");
        result.OwnerEntityId.ShouldBe(ownerEntityId, "Owner entity ID should be preserved");
        result.Email.ShouldBe(activeAccount.Email, "Email should be preserved");
        result.LastUpdated.ShouldBeGreaterThan(timestampBefore, "LastUpdated should be newer after deactivation");

        // Cleanup
        await ApiDataContext.Remove<Account>(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Verify that Register creates a new account with hashed password and inactive state.
    /// PURPOSE: Ensures the registration workflow correctly validates input and persists the account.
    /// BUSINESS CONTEXT: User registration is the entry point for new users to the system.
    /// WHY IMPORTANT: Registered accounts must have hashed passwords and start in inactive state.
    /// ARCHITECTURAL SIGNIFICANCE: Tests validation, password hashing, and proper field initialization.
    /// FUTURE RESILIENCE: Validates registration workflow continues to work after security changes.
    /// </summary>
    [Fact]
    public async Task Register_WithValidData_CreatesInactiveAccountWithHashedPassword()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var email = $"test_{Guid.NewGuid()}@example.com";
        var plainPassword = "SecurePassword123!";

        var newAccount = new Account
        {
            Email = email,
            Password = plainPassword
        };

        var accountHandler = ApiDataContext.For<AccountHandler>(ownerEntityId);

        // Act
        var result = await accountHandler.Register(newAccount);

        // Assert - verify account was created correctly
        result.ShouldNotBeNull("Register should return the created account");
        result.Email.ShouldBe(email, "Email should match input");
        result.IsActive.ShouldBeFalse("New account should start as inactive");
        result.Password.ShouldBeEmpty("Plain password should be cleared");
        result.PasswordHash.ShouldNotBeNullOrWhiteSpace("Password hash should be set");
        result.PasswordHash.ShouldNotBe(plainPassword, "Password hash should not equal plain password");
        result.OwnerEntityId.ShouldBe(ownerEntityId, "Owner entity ID should be set to handler context");
        result.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue, "CreatedAt should be set");
        result.LastUpdated.ShouldBeGreaterThan(DateTime.MinValue, "LastUpdated should be set");

        // Verify password hash is valid BCrypt format
        result.PasswordHash.ShouldStartWith("$2");

        // Cleanup
        await ApiDataContext.Remove<Account>(ownerEntityId);
    }
}
