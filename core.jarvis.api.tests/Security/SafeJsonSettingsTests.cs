using core.jarvis.api.Security;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Security;

/// <summary>
/// Tests for SafeJsonSettings to verify secure JSON deserialization.
///
/// INTENT: Verify JSON deserialization is protected against RCE attacks
/// PURPOSE: Prevent TypeNameHandling-based deserialization attacks
/// BUSINESS CONTEXT: Insecure deserialization is OWASP Top 10 vulnerability
/// WHY IMPORTANT: TypeNameHandling.Objects/All can lead to Remote Code Execution
/// ARCHITECTURAL SIGNIFICANCE: Validates security at serialization boundary
/// FUTURE RESILIENCE: Ensures all JSON processing uses safe settings
/// </summary>
public class SafeJsonSettingsTests
{
    #region TypeNameHandling Security Tests (jarvis-45)

    /// <summary>
    /// INTENT: Verify TypeNameHandling is set to None to prevent RCE attacks.
    /// SECURITY: TypeNameHandling.Objects/All allows arbitrary type instantiation.
    /// </summary>
    [Fact]
    public void SafeJsonSettings_HasTypeNameHandlingNone()
    {
        // Assert
        SafeJsonSettings.Default.TypeNameHandling.ShouldBe(TypeNameHandling.None);
    }

    /// <summary>
    /// INTENT: Verify MaxDepth is set to prevent stack overflow attacks.
    /// SECURITY: Deeply nested JSON can cause stack overflow.
    /// </summary>
    [Fact]
    public void SafeJsonSettings_HasMaxDepthLimit()
    {
        // Assert
        SafeJsonSettings.Default.MaxDepth.ShouldBe(32);
    }

    /// <summary>
    /// INTENT: Verify malicious $type payloads don't cause arbitrary type instantiation.
    /// SECURITY: Attackers include $type to instantiate arbitrary types for RCE.
    /// With TypeNameHandling.None, $type is ignored entirely.
    /// </summary>
    [Fact]
    public void Deserialize_MaliciousTypePayload_DoesNotInstantiateArbitraryTypes()
    {
        // Arrange - Payload with $type that would execute code if TypeNameHandling was enabled
        var maliciousJson = @"{
            ""$type"": ""System.Windows.Data.ObjectDataProvider, PresentationFramework"",
            ""MethodName"": ""Start"",
            ""MethodParameters"": {
                ""$type"": ""System.Collections.ArrayList"",
                ""$values"": [""cmd"", ""/c calc.exe""]
            },
            ""ObjectInstance"": {
                ""$type"": ""System.Diagnostics.Process, System""
            }
        }";

        // Act - Should deserialize as a simple object, ignoring $type
        var result = SafeJsonSettings.Deserialize<Dictionary<string, object>>(maliciousJson);

        // Assert - With TypeNameHandling.None, $type is ignored completely
        // The object is deserialized as a plain dictionary with MethodName, MethodParameters, ObjectInstance
        result.ShouldNotBeNull();
        result.ContainsKey("MethodName").ShouldBeTrue();
        result["MethodName"].ShouldBe("Start");
        // No arbitrary type was instantiated - we get simple objects, not Process/ObjectDataProvider
    }

    /// <summary>
    /// INTENT: Verify $type in JSON doesn't cause dangerous type instantiation.
    /// SECURITY: Even with $type present, we should get simple objects only.
    /// </summary>
    [Fact]
    public void Deserialize_TypePayload_ReturnsSimpleTypes()
    {
        // Arrange - JSON with $type that would be dangerous if TypeNameHandling was enabled
        var jsonWithType = @"{""$type"": ""System.Diagnostics.Process, System"", ""Name"": ""test"", ""Value"": 42}";

        // Act
        var result = SafeJsonSettings.Deserialize<TestModel>(jsonWithType);

        // Assert - We get TestModel, not Process
        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestModel>();
        result.Name.ShouldBe("test");
        result.Value.ShouldBe(42);
    }

    /// <summary>
    /// INTENT: Verify normal JSON deserialization works correctly.
    /// </summary>
    [Fact]
    public void Deserialize_ValidJson_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{""Name"": ""Test"", ""Value"": 42}";

        // Act
        var result = SafeJsonSettings.Deserialize<TestModel>(json);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Value.ShouldBe(42);
    }

    /// <summary>
    /// INTENT: Verify null/empty JSON returns default value.
    /// </summary>
    [Fact]
    public void Deserialize_NullOrEmptyJson_ReturnsDefault()
    {
        // Act & Assert
        SafeJsonSettings.Deserialize<TestModel>(null).ShouldBeNull();
        SafeJsonSettings.Deserialize<TestModel>("").ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify Serialize produces valid JSON.
    /// </summary>
    [Fact]
    public void Serialize_ValidObject_ProducesJson()
    {
        // Arrange
        var obj = new TestModel { Name = "Test", Value = 42 };

        // Act
        var json = SafeJsonSettings.Serialize(obj);

        // Assert
        json.ShouldContain("\"Name\":\"Test\"");
        json.ShouldContain("\"Value\":42");
    }

    /// <summary>
    /// INTENT: Verify serialization does not include $type metadata.
    /// SECURITY: Type metadata should never be serialized to prevent confusion.
    /// </summary>
    [Fact]
    public void Serialize_DoesNotIncludeTypeMetadata()
    {
        // Arrange
        var obj = new TestModel { Name = "Test", Value = 42 };

        // Act
        var json = SafeJsonSettings.Serialize(obj);

        // Assert
        json.ShouldNotContain("$type");
    }

    #endregion

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
