using core.jarvis.api.Middleware;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using System;
using System.Reflection;

namespace core.jarvis.api.tests.Integration.Functions;

/// <summary>
/// INTENT: Verify component validation middleware functionality
/// PURPOSE: Ensure middleware correctly validates IComponent and GUID requests
/// BUSINESS CONTEXT: Enforces type safety at API boundary
/// WHY IMPORTANT: Prevents invalid data from entering the system
/// ARCHITECTURAL SIGNIFICANCE: Validates ECS component pattern compliance
/// FUTURE RESILIENCE: Protects against non-compliant request formats
/// </summary>
public class ComponentValidationMiddlewareTests
{
    private readonly ILogger<ComponentValidationMiddleware> _logger;
    private readonly ComponentValidationMiddleware _middleware;

    public ComponentValidationMiddlewareTests()
    {
        _logger = new NullLogger<ComponentValidationMiddleware>();
        _middleware = new ComponentValidationMiddleware(_logger);
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Valid_Guid_Should_Return_True()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var body = $"\"{guid}\"";

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Valid_Component_Should_Return_True()
    {
        // Arrange
        var component = new Account
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow,
            Email = "test@example.com",
            Password = "password"
        };
        var body = JsonConvert.SerializeObject(component);

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Component_Missing_Id_Should_Return_False()
    {
        // Arrange
        var body = @"{
            ""ownerEntityId"": ""00000000-0000-0000-0000-000000000000"",
            ""updatedAt"": ""2024-01-01T00:00:00Z"",
            ""email"": ""test@example.com""
        }";

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Component_Missing_OwnerEntityId_Should_Return_False()
    {
        // Arrange
        var body = @"{
            ""id"": ""00000000-0000-0000-0000-000000000000"",
            ""updatedAt"": ""2024-01-01T00:00:00Z"",
            ""email"": ""test@example.com""
        }";

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Component_Missing_UpdatedAt_Should_Return_False()
    {
        // Arrange
        var body = @"{
            ""id"": ""00000000-0000-0000-0000-000000000000"",
            ""ownerEntityId"": ""00000000-0000-0000-0000-000000000000"",
            ""email"": ""test@example.com""
        }";

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Invalid_Json_Should_Return_False()
    {
        // Arrange
        var body = "{ invalid json }";

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Plain_String_Should_Return_False()
    {
        // Arrange
        var body = "just a plain string";

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidComponentOrGuid_With_Case_Insensitive_Properties_Should_Return_True()
    {
        // Arrange
        var body = @"{
            ""Id"": ""00000000-0000-0000-0000-000000000000"",
            ""OwnerEntityId"": ""00000000-0000-0000-0000-000000000000"",
            ""UpdatedAt"": ""2024-01-01T00:00:00Z""
        }";

        // Act
        var result = InvokePrivateMethod<bool>(_middleware, "IsValidComponentOrGuid", body);

        // Assert
        result.ShouldBeTrue();
    }

    // Helper method to invoke private methods using reflection
    private static T InvokePrivateMethod<T>(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found");
            
        var result = method.Invoke(instance, parameters);
        return (T)(result ?? throw new InvalidOperationException("Method returned null"));
    }
}

// NullLogger implementation
public class NullLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    
    private class NullScope : IDisposable
    {
        public void Dispose() { }
    }
}