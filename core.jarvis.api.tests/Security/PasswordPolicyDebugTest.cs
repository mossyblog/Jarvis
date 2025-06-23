using System;
using core.jarvis.api.Services;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace core.jarvis.api.tests.Security;

public class PasswordPolicyDebugTest
{
    private readonly ITestOutputHelper _output;

    public PasswordPolicyDebugTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_PasswordPolicy_Score()
    {
        var policy = new PasswordPolicyService();
        var password = "MySecure#Pass123!";
        
        var result = policy.ValidatePassword(password, "test@example.com");
        
        _output.WriteLine($"Password: {password}");
        _output.WriteLine($"IsValid: {result.IsValid}");
        _output.WriteLine($"Score: {result.Score}");
        _output.WriteLine($"Strength: {result.Strength}");
        _output.WriteLine($"Errors: {string.Join(", ", result.Errors)}");
        
        // Let's verify the password meets all requirements
        password.Length.ShouldBeGreaterThanOrEqualTo(8);
        password.Any(char.IsUpper).ShouldBeTrue();
        password.Any(char.IsLower).ShouldBeTrue();
        password.Any(char.IsDigit).ShouldBeTrue();
        password.Any(c => !char.IsLetterOrDigit(c)).ShouldBeTrue();
        
        // This should help us understand why it's failing
        result.IsValid.ShouldBeTrue($"Score: {result.Score}, Errors: {string.Join(", ", result.Errors)}");
    }
}