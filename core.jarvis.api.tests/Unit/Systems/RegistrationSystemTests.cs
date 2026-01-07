using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.Systems;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using core.jarvis.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Unit.Systems;

/// <summary>
/// INTENT: Test RegistrationSystem user registration workflow
/// PURPOSE: Verify registration creates Account and SecurityProfile atomically
/// BUSINESS CONTEXT: New users must be able to register with email and password
/// WHY IMPORTANT: Registration is the entry point for user onboarding
/// ARCHITECTURAL SIGNIFICANCE: Tests transactional component creation
/// FUTURE RESILIENCE: Prevents registration workflow regressions
/// </summary>
public class RegistrationSystemTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Test successful user registration with valid credentials
    /// PURPOSE: Verify Account and SecurityProfile are created together
    /// BUSINESS CONTEXT: Valid registration should create both components
    /// WHY IMPORTANT: Core registration flow must work end-to-end
    /// ARCHITECTURAL SIGNIFICANCE: Tests transactional commit across handlers
    /// FUTURE RESILIENCE: Catches issues in component creation chain
    /// </summary>
    [Fact]
    public async Task RegisterUser_WithValidCredentials_CreatesAccountAndSecurityProfile()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var fullName = "Test User";
        var ipAddress = "127.0.0.1";

        var requestBody = JsonSerializer.Serialize(new
        {
            email,
            password,
            fullName
        });

        var registrationSystem = _serviceProvider.GetRequiredService<RegistrationSystem>();

        // Act
        var components = await registrationSystem.RegisterUser(requestBody, ipAddress);

        // Assert - Should return both Account and SecurityProfile
        components.ShouldNotBeNull();
        components.Count.ShouldBe(2);

        var account = components.OfType<Account>().FirstOrDefault();
        var profile = components.OfType<SecurityProfile>().FirstOrDefault();

        account.ShouldNotBeNull();
        account.Email.ShouldBe(email.ToLower().Trim());
        account.IsActive.ShouldBeTrue();
        account.AuthMethod.ShouldBe("password");
        account.PasswordHash.ShouldNotBeNullOrEmpty();
        account.Password.ShouldBe(string.Empty); // Plain password should never be stored
        account.IpAddress.ShouldBe(ipAddress);

        profile.ShouldNotBeNull();
        profile.Name.ShouldBe(fullName);
        profile.OwnerEntityId.ShouldBe(account.OwnerEntityId);

        // Track for cleanup
        TrackEntity(account.OwnerEntityId);
    }

    /// <summary>
    /// INTENT: Test registration fails with invalid email format
    /// PURPOSE: Verify email validation rejects malformed addresses
    /// BUSINESS CONTEXT: Invalid emails should not create accounts
    /// WHY IMPORTANT: Data quality and user experience
    /// ARCHITECTURAL SIGNIFICANCE: Tests validation layer before database
    /// FUTURE RESILIENCE: Ensures validation rules are enforced
    /// </summary>
    [Fact]
    public async Task RegisterUser_WithInvalidEmail_ThrowsValidationException()
    {
        // Arrange
        var invalidEmail = "not-an-email";
        var password = "TestPassword123!";

        var requestBody = JsonSerializer.Serialize(new
        {
            email = invalidEmail,
            password
        });

        var registrationSystem = _serviceProvider.GetRequiredService<RegistrationSystem>();

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await registrationSystem.RegisterUser(requestBody, "127.0.0.1")
        );

        exception.Errors.ShouldContainKey("email");
        exception.Errors["email"].ShouldContain(e => e.Contains("Invalid email format"));
    }

    /// <summary>
    /// INTENT: Test registration fails with weak password
    /// PURPOSE: Verify password policy is enforced during registration
    /// BUSINESS CONTEXT: Security requires strong passwords
    /// WHY IMPORTANT: Prevents accounts with easily compromised credentials
    /// ARCHITECTURAL SIGNIFICANCE: Tests IPasswordPolicyService integration
    /// FUTURE RESILIENCE: Ensures password policy changes are tested
    /// </summary>
    [Fact]
    public async Task RegisterUser_WithWeakPassword_ThrowsValidationException()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var weakPassword = "weak"; // Too short, missing character types

        var requestBody = JsonSerializer.Serialize(new
        {
            email,
            password = weakPassword
        });

        var registrationSystem = _serviceProvider.GetRequiredService<RegistrationSystem>();

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await registrationSystem.RegisterUser(requestBody, "127.0.0.1")
        );

        exception.Errors.ShouldContainKey("password");
        exception.Errors["password"].Length.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// INTENT: Test registration fails when email already exists
    /// PURPOSE: Verify duplicate email prevention
    /// BUSINESS CONTEXT: Each email can only have one account
    /// WHY IMPORTANT: Prevents account confusion and security issues
    /// ARCHITECTURAL SIGNIFICANCE: Tests email uniqueness check against database
    /// FUTURE RESILIENCE: Ensures email deduplication works correctly
    /// </summary>
    [Fact]
    public async Task RegisterUser_WithExistingEmail_ThrowsBusinessRuleException()
    {
        // Arrange - First create an account with the email
        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var userEntityId = Guid.NewGuid();

        TrackEntity(userEntityId);

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var existingAccount = new Account
        {
            OwnerEntityId = userEntityId,
            Email = email.ToLower(),
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(existingAccount);

        // Now try to register with the same email
        var requestBody = JsonSerializer.Serialize(new
        {
            email,
            password
        });

        var registrationSystem = _serviceProvider.GetRequiredService<RegistrationSystem>();

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(
            async () => await registrationSystem.RegisterUser(requestBody, "127.0.0.1")
        );

        exception.Code.ShouldContain("EMAIL_EXISTS");
    }
}
