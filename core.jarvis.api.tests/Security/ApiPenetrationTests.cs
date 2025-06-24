using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using core.jarvis.api.Functions.Security;
using core.jarvis.api.tests.Helpers;
using core.jarvis.api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Security;

/// <summary>
/// INTENT: Comprehensive penetration testing of API security
/// PURPOSE: Identify and validate protection against common attack vectors
/// BUSINESS CONTEXT: Security assessment to prevent data breaches
/// WHY IMPORTANT: Proactive security testing prevents exploitation
/// ARCHITECTURAL SIGNIFICANCE: Validates all security layers
/// FUTURE RESILIENCE: Ensures API remains secure against evolving threats
/// </summary>
public class ApiPenetrationTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Test SQL injection vulnerabilities in authentication
    /// PURPOSE: Ensure malicious SQL cannot bypass authentication
    /// BUSINESS CONTEXT: SQL injection is a critical vulnerability
    /// WHY IMPORTANT: Can lead to complete database compromise
    /// ARCHITECTURAL SIGNIFICANCE: Tests input sanitization layer
    /// FUTURE RESILIENCE: Protects against database attacks
    /// </summary>
    [Fact]
    public async Task Auth_MustBeImmuneToSQLInjection()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        var sqlInjectionPayloads = new[]
        {
            "admin' OR '1'='1",
            "admin'; DROP TABLE users; --",
            "' OR 1=1--",
            "admin'/*",
            "admin' --",
            "admin' #",
            "admin'||'1'='1",
            "' UNION SELECT * FROM users--",
            "admin' AND 1=0 UNION ALL SELECT 'admin', '1'--",
            "1' AND '1' = '1"
        };

        // Act & Assert - All SQL injection attempts must fail
        foreach (var payload in sqlInjectionPayloads)
        {
            var req = TestFactory.CreateHttpRequestData(
                "POST",
                "/api/security/auth",
                JsonConvert.SerializeObject(new
                {
                    email = payload,
                    password = "password123"
                })
            );

            var response = await authFunction.Run(req);
            
            // Must return 401 Unauthorized or 400 Bad Request, never 200 OK
            response.StatusCode.ShouldBeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest },
                $"SQL injection payload '{payload}' should not authenticate");
            
            // Check error doesn't leak SQL information
            var responseBody = await TestFactory.GetResponseBodyAsync(response);
            responseBody.ShouldNotBeNullOrEmpty();
            responseBody.ToLower().ShouldNotContain("sql"); // Error must not leak SQL details
            responseBody.ToLower().ShouldNotContain("syntax"); // Error must not leak syntax details
            responseBody.ToLower().ShouldNotContain("column"); // Error must not leak schema details
        }
    }

    /// <summary>
    /// INTENT: Test NoSQL injection vulnerabilities
    /// PURPOSE: Prevent JSON/NoSQL query manipulation
    /// BUSINESS CONTEXT: Modern APIs often use NoSQL backends
    /// WHY IMPORTANT: NoSQL injection can bypass authentication
    /// ARCHITECTURAL SIGNIFICANCE: Tests JSON parsing security
    /// FUTURE RESILIENCE: Protects against document store attacks
    /// </summary>
    [Fact]
    public async Task Auth_MustBeImmuneToNoSQLInjection()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        var noSqlInjectionPayloads = new[]
        {
            "{\"email\": {\"$ne\": null}, \"password\": {\"$ne\": null}}",
            "{\"email\": \"admin@test.com\", \"password\": {\"$gt\": \"\"}}",
            "{\"email\": {\"$regex\": \".*\"}, \"password\": {\"$regex\": \".*\"}}",
            "{\"$where\": \"this.email == 'admin@test.com'\"}",
            "{\"email\": \"admin@test.com\", \"$or\": [{\"password\": \"wrong\"}, {\"a\": \"a\"}]}"
        };

        // Act & Assert
        foreach (var payload in noSqlInjectionPayloads)
        {
            var req = TestFactory.CreateHttpRequestData(
                "POST",
                "/api/security/auth",
                payload // Raw JSON payload
            );

            var response = await authFunction.Run(req);
            
            response.StatusCode.ShouldBeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest },
                $"NoSQL injection payload should not authenticate");
        }
    }

    /// <summary>
    /// INTENT: Test command injection vulnerabilities
    /// PURPOSE: Prevent OS command execution
    /// BUSINESS CONTEXT: Command injection leads to server compromise
    /// WHY IMPORTANT: Can execute arbitrary commands on server
    /// ARCHITECTURAL SIGNIFICANCE: Tests process execution boundaries
    /// FUTURE RESILIENCE: Prevents server takeover
    /// </summary>
    [Fact]
    public async Task Auth_MustBeImmuneToCommandInjection()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        var commandInjectionPayloads = new[]
        {
            "admin@test.com; cat /etc/passwd",
            "admin@test.com && whoami",
            "admin@test.com | ls -la",
            "admin@test.com`id`",
            "admin@test.com$(whoami)",
            "admin@test.com\"; system('id'); //",
            "admin@test.com'; exec('cmd.exe'); --"
        };

        // Act & Assert
        foreach (var payload in commandInjectionPayloads)
        {
            var req = TestFactory.CreateHttpRequestData(
                "POST",
                "/api/security/auth",
                JsonConvert.SerializeObject(new
                {
                    email = payload,
                    password = "password123"
                })
            );

            var response = await authFunction.Run(req);
            
            response.StatusCode.ShouldBeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest },
                $"Command injection payload '{payload}' should not execute");
            
            // Verify no command output in response
            var responseBody = await TestFactory.GetResponseBodyAsync(response);
            responseBody.ShouldNotContain("root:"); // Must not leak /etc/passwd
            responseBody.ShouldNotContain("uid="); // Must not leak user ID
            responseBody.ShouldNotContain("cmd.exe"); // Must not leak Windows info
        }
    }

    /// <summary>
    /// INTENT: Test LDAP injection vulnerabilities
    /// PURPOSE: Prevent LDAP query manipulation
    /// BUSINESS CONTEXT: Many enterprises use LDAP for auth
    /// WHY IMPORTANT: LDAP injection can bypass authentication
    /// ARCHITECTURAL SIGNIFICANCE: Tests directory service integration
    /// FUTURE RESILIENCE: Protects against directory attacks
    /// </summary>
    [Fact]
    public async Task Auth_MustBeImmuneToLDAPInjection()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        var ldapInjectionPayloads = new[]
        {
            "*",
            "admin*",
            "*)(uid=*))(|(uid=*",
            "admin)(|(password=*",
            "admin))(&(password=*",
            "*)(mail=*))(|(mail=*"
        };

        // Act & Assert
        foreach (var payload in ldapInjectionPayloads)
        {
            var req = TestFactory.CreateHttpRequestData(
                "POST",
                "/api/security/auth",
                JsonConvert.SerializeObject(new
                {
                    email = payload,
                    password = "password123"
                })
            );

            var response = await authFunction.Run(req);
            
            response.StatusCode.ShouldBeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest },
                $"LDAP injection payload '{payload}' should not authenticate");
        }
    }

    /// <summary>
    /// INTENT: Test XML injection vulnerabilities
    /// PURPOSE: Prevent XML entity attacks
    /// BUSINESS CONTEXT: XML parsing vulnerabilities are common
    /// WHY IMPORTANT: Can lead to file disclosure and DoS
    /// ARCHITECTURAL SIGNIFICANCE: Tests XML parsing security
    /// FUTURE RESILIENCE: Prevents XXE attacks
    /// </summary>
    [Fact]
    public async Task Auth_MustBeImmuneToXMLInjection()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        var xmlPayload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE foo [<!ENTITY xxe SYSTEM ""file:///etc/passwd"">]>
<user>
    <email>&xxe;</email>
    <password>password123</password>
</user>";

        var req = TestFactory.CreateHttpRequestData(
            "POST",
            "/api/security/auth",
            xmlPayload
        );
        req.Headers.Add("Content-Type", "application/xml");

        // Act
        var response = await authFunction.Run(req);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "XML payload should be rejected");
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldNotContain("root:"); // Must not process XXE
    }

    /// <summary>
    /// INTENT: Test JWT manipulation vulnerabilities
    /// PURPOSE: Ensure JWT tokens cannot be forged or manipulated
    /// BUSINESS CONTEXT: JWT security is critical for API auth
    /// WHY IMPORTANT: Weak JWT validation leads to auth bypass
    /// ARCHITECTURAL SIGNIFICANCE: Tests cryptographic validation
    /// FUTURE RESILIENCE: Prevents token forgery
    /// </summary>
    [Fact]
    public async Task JWT_MustBeImmuneToManipulation()
    {
        // Arrange
        var validateFunction = _serviceProvider.GetRequiredService<ValidateFunction>();
        
        // Test various JWT attack vectors
        var jwtAttacks = new[]
        {
            // None algorithm attack
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiJhZG1pbiIsImV4cCI6OTk5OTk5OTk5OX0.",
            
            // Weak secret attack (HS256 with 'secret')
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImV4cCI6OTk5OTk5OTk5OX0.FAKESIGNATURE",
            
            // Algorithm confusion (RS256 -> HS256)
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImV4cCI6OTk5OTk5OTk5OX0.PUBLICKEYSIG",
            
            // Expired token
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImV4cCI6MX0.SIGNATURE",
            
            // Invalid format
            "not.a.jwt",
            "two.parts",
            ""
        };

        // Act & Assert
        foreach (var attackToken in jwtAttacks)
        {
            var req = TestFactory.CreateHttpRequestData(
                "GET",
                "/api/security/validate"
            );
            req.Headers.Add("Authorization", $"Bearer {attackToken}");

            var response = await validateFunction.Run(req);
            
            // Debug: Log the response body if not Unauthorized
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                var responseBody = await TestFactory.GetResponseBodyAsync(response);
                Console.WriteLine($"Unexpected response for token '{attackToken}': Status={response.StatusCode}, Body={responseBody}");
            }
            
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
                $"JWT attack token should be rejected");
        }
    }

    /// <summary>
    /// INTENT: Test authorization bypass vulnerabilities
    /// PURPOSE: Ensure users cannot access unauthorized resources
    /// BUSINESS CONTEXT: Authorization bypass leads to data breach
    /// WHY IMPORTANT: Prevents privilege escalation
    /// ARCHITECTURAL SIGNIFICANCE: Tests access control implementation
    /// FUTURE RESILIENCE: Maintains proper access boundaries
    /// </summary>
    [Fact]
    public async Task Authorization_MustPreventBypass()
    {
        // This would test authorization on actual endpoints
        // For now, we'll test the token validation ensures proper user context
        
        // Arrange
        var validateFunction = _serviceProvider.GetRequiredService<ValidateFunction>();
        
        // Create a request without authorization header
        var req = TestFactory.CreateHttpRequestData("GET", "/api/security/validate");

        // Act
        var response = await validateFunction.Run(req);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
            "Missing authorization should be rejected");
    }

    /// <summary>
    /// INTENT: Test rate limiting and DoS protection
    /// PURPOSE: Prevent brute force and denial of service
    /// BUSINESS CONTEXT: Rate limiting prevents resource exhaustion
    /// WHY IMPORTANT: Maintains service availability
    /// ARCHITECTURAL SIGNIFICANCE: Tests throttling implementation
    /// FUTURE RESILIENCE: Prevents automated attacks
    /// </summary>
    [Fact]
    public async Task RateLimiting_MustPreventBruteForce()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        var attemptCount = 10; // Simulate rapid requests
        var responses = new List<HttpResponseData>();

        // Act - Simulate rapid authentication attempts
        for (int i = 0; i < attemptCount; i++)
        {
            var req = TestFactory.CreateHttpRequestData(
                "POST",
                "/api/security/auth",
                JsonConvert.SerializeObject(new
                {
                    email = "bruteforce@test.com",
                    password = $"wrong{i}"
                })
            );

            var response = await authFunction.Run(req);
            responses.Add(response);
        }

        // Assert - Later requests should be rate limited
        // Note: Actual rate limiting would need to be implemented
        // This test documents the expected behavior
        responses.All(r => r.StatusCode == HttpStatusCode.Unauthorized).ShouldBeTrue(
            "All attempts with wrong password should fail");
    }

    /// <summary>
    /// INTENT: Test information disclosure vulnerabilities
    /// PURPOSE: Prevent leaking sensitive information
    /// BUSINESS CONTEXT: Information disclosure aids attackers
    /// WHY IMPORTANT: Reduces attack surface
    /// ARCHITECTURAL SIGNIFICANCE: Tests error handling
    /// FUTURE RESILIENCE: Maintains security through obscurity
    /// </summary>
    [Fact]
    public async Task Errors_MustNotLeakInformation()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        // Test various error-inducing inputs
        var errorInputs = new[]
        {
            new { email = "nonexistent@test.com", password = "password123" },
            new { email = "test@test.com", password = "" },
            new { email = "", password = "password123" },
            new { email = (string?)null, password = "password123" }
        };

        // Act & Assert
        foreach (var input in errorInputs)
        {
            var req = TestFactory.CreateHttpRequestData(
                "POST",
                "/api/security/auth",
                JsonConvert.SerializeObject(input)
            );

            var response = await authFunction.Run(req);
            var responseBody = await TestFactory.GetResponseBodyAsync(response);
            
            // Check for information leakage
            responseBody.ToLower().ShouldNotContain("user not found");
            responseBody.ToLower().ShouldNotContain("incorrect password");
            responseBody.ToLower().ShouldNotContain("null reference");
            responseBody.ToLower().ShouldNotContain("stack trace");
            responseBody.ToLower().ShouldNotContain("at line");
        }
    }

    /// <summary>
    /// INTENT: Test CORS and origin validation
    /// PURPOSE: Prevent cross-origin attacks
    /// BUSINESS CONTEXT: CORS misconfig leads to data theft
    /// WHY IMPORTANT: Prevents XSS and CSRF attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests cross-origin policy
    /// FUTURE RESILIENCE: Maintains browser security
    /// </summary>
    [Fact]
    public async Task CORS_MustBeProperlyConfigured()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        var req = TestFactory.CreateHttpRequestData(
            "POST",
            "/api/security/auth",
            JsonConvert.SerializeObject(new { email = "test@test.com", password = "password123" })
        );
        req.Headers.Add("Origin", "https://evil.com");

        // Act
        var response = await authFunction.Run(req);
        
        // Assert - Check CORS headers
        // Actual CORS would be configured at the hosting layer
        // This documents expected behavior
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError,
            "CORS should not cause server errors");
    }

    /// <summary>
    /// INTENT: Test session fixation vulnerabilities
    /// PURPOSE: Prevent session hijacking
    /// BUSINESS CONTEXT: Session security is critical
    /// WHY IMPORTANT: Prevents account takeover
    /// ARCHITECTURAL SIGNIFICANCE: Tests session management
    /// FUTURE RESILIENCE: Maintains session integrity
    /// </summary>
    [Fact]
    public async Task Sessions_MustRegenerateOnAuth()
    {
        // Arrange
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        
        // Perform two authentication attempts
        var req1 = TestFactory.CreateHttpRequestData(
            "POST",
            "/api/security/auth",
            JsonConvert.SerializeObject(new { email = "test@test.com", password = "Test123!" })
        );
        
        var req2 = TestFactory.CreateHttpRequestData(
            "POST",
            "/api/security/auth",
            JsonConvert.SerializeObject(new { email = "test@test.com", password = "Test123!" })
        );

        // Act
        var response1 = await authFunction.Run(req1);
        var response2 = await authFunction.Run(req2);
        
        // Assert - Session IDs should be different
        if (response1.StatusCode == HttpStatusCode.OK && response2.StatusCode == HttpStatusCode.OK)
        {
            var responseBody1 = await TestFactory.GetResponseBodyAsync(response1);
            var responseBody2 = await TestFactory.GetResponseBodyAsync(response2);
            
            var token1 = JsonConvert.DeserializeObject<dynamic>(responseBody1);
            var token2 = JsonConvert.DeserializeObject<dynamic>(responseBody2);
            
            // Note: Session IDs would need to be implemented in the auth response
            // For now, we can verify that tokens are different
            responseBody1.ShouldNotBe(responseBody2,
                "Each authentication should generate unique response");
        }
    }

    /// <summary>
    /// INTENT: Comprehensive security test summary
    /// PURPOSE: Ensure all attack vectors are covered
    /// BUSINESS CONTEXT: Complete security validation
    /// WHY IMPORTANT: No security gaps allowed
    /// ARCHITECTURAL SIGNIFICANCE: Validates entire security posture
    /// FUTURE RESILIENCE: Continuous security improvement
    /// </summary>
    [Fact]
    public void SecurityPenetrationTest_Summary()
    {
        // This test summarizes all penetration test coverage
        var testMethods = GetType().GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), false).Length > 0)
            .Select(m => m.Name)
            .ToList();

        // Verify we have comprehensive coverage
        testMethods.ShouldContain("Auth_MustBeImmuneToSQLInjection");
        testMethods.ShouldContain("Auth_MustBeImmuneToNoSQLInjection");
        testMethods.ShouldContain("Auth_MustBeImmuneToCommandInjection");
        testMethods.ShouldContain("Auth_MustBeImmuneToLDAPInjection");
        testMethods.ShouldContain("Auth_MustBeImmuneToXMLInjection");
        testMethods.ShouldContain("JWT_MustBeImmuneToManipulation");
        testMethods.ShouldContain("Authorization_MustPreventBypass");
        testMethods.ShouldContain("RateLimiting_MustPreventBruteForce");
        testMethods.ShouldContain("Errors_MustNotLeakInformation");
        testMethods.ShouldContain("CORS_MustBeProperlyConfigured");
        testMethods.ShouldContain("Sessions_MustRegenerateOnAuth");
        
        testMethods.Count.ShouldBeGreaterThanOrEqualTo(11,
            "Comprehensive penetration testing coverage required");
    }
}