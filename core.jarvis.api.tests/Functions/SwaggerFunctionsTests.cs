using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using core.jarvis.api.tests.Helpers;

namespace core.jarvis.api.tests.Functions;

public class SwaggerFunctionsTests
{
    private readonly Mock<ILogger<core.jarvis.api.Functions.SwaggerFunctions>> _loggerMock;
    private readonly core.jarvis.api.Functions.SwaggerFunctions _function;
    private readonly Mock<FunctionContext> _contextMock;

    public SwaggerFunctionsTests()
    {
        _loggerMock = new Mock<ILogger<core.jarvis.api.Functions.SwaggerFunctions>>();
        _function = new core.jarvis.api.Functions.SwaggerFunctions(_loggerMock.Object);
        _contextMock = new Mock<FunctionContext>();
    }

    [Fact]
    public async Task RenderSwaggerUI_ShouldReturnHtmlContent()
    {
        // Arrange
        var request = TestFactory.CreateHttpRequestData("GET", "http://localhost/api/swagger/ui");

        // Act
        var response = await _function.RenderSwaggerUI(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Type").First().ShouldBe("text/html; charset=utf-8");
        
        var content = await TestFactory.GetResponseBodyAsync(response);
        content.ShouldNotBeNull();
        content.ShouldContain("<!DOCTYPE html>");
        content.ShouldContain("swagger-ui");
        content.ShouldContain("/api/swagger.json");
    }

    [Theory]
    [InlineData("json", "application/json")]
    [InlineData("yaml", "text/yaml")]
    [InlineData("yml", "text/yaml")]
    public async Task RenderOpenApiDocument_ShouldReturnCorrectContentType(string extension, string expectedContentType)
    {
        // Arrange
        var request = TestFactory.CreateHttpRequestData("GET", $"http://localhost/api/swagger.{extension}");

        // Act
        var response = await _function.RenderOpenApiDocument(request, extension);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Type").First().ShouldBe(expectedContentType);
        
        var content = await TestFactory.GetResponseBodyAsync(response);
        content.ShouldNotBeNull();
        content.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RenderOpenApiDocument_Json_ShouldReturnValidOpenApiDocument()
    {
        // Arrange
        var request = TestFactory.CreateHttpRequestData("GET", "http://localhost/api/swagger.json");

        // Act
        var response = await _function.RenderOpenApiDocument(request, "json");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await TestFactory.GetResponseBodyAsync(response);
        content.ShouldNotBeNull();
        
        // Verify it's valid JSON and contains OpenAPI structure
        content.ShouldContain("\"openapi\":");
        content.ShouldContain("\"info\":");
        content.ShouldContain("\"paths\":");
        content.ShouldContain("\"/security/auth\":");
        content.ShouldContain("\"/security/deauth\":");
        content.ShouldContain("\"/security/refresh\":");
        content.ShouldContain("\"/security/validate\":");
        content.ShouldContain("\"components\":");
        content.ShouldContain("\"schemas\":");
    }

    [Fact]
    public async Task RenderOpenApiDocument_Yaml_ShouldReturnValidYamlDocument()
    {
        // Arrange
        var request = TestFactory.CreateHttpRequestData("GET", "http://localhost/api/swagger.yaml");

        // Act
        var response = await _function.RenderOpenApiDocument(request, "yaml");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await TestFactory.GetResponseBodyAsync(response);
        content.ShouldNotBeNull();
        
        // Verify it's valid YAML and contains OpenAPI structure
        content.ShouldContain("openapi:");
        content.ShouldContain("info:");
        content.ShouldContain("paths:");
        content.ShouldContain("/security/auth:");
        content.ShouldContain("/security/deauth:");
        content.ShouldContain("/security/refresh:");
        content.ShouldContain("/security/validate:");
        content.ShouldContain("components:");
        content.ShouldContain("schemas:");
    }

    [Fact(Skip = "Schema validation needs update after ECS refactoring")]
    public async Task RenderOpenApiDocument_ShouldIncludeAllSchemas()
    {
        // Arrange
        var request = TestFactory.CreateHttpRequestData("GET", "http://localhost/api/swagger.json");

        // Act
        var response = await _function.RenderOpenApiDocument(request, "json");

        // Assert
        var content = await TestFactory.GetResponseBodyAsync(response);
        content.ShouldNotBeNull();
        
        // Verify all component schemas are included
        content.ShouldContain("\"Auth\":");
        content.ShouldContain("\"User\":");
        content.ShouldContain("\"Role\":");
        content.ShouldContain("\"Permission\":");
        content.ShouldContain("\"TokenValidation\":");
    }

    [Fact]
    public async Task RenderOpenApiDocument_ShouldHandleUnknownExtension()
    {
        // Arrange
        var request = TestFactory.CreateHttpRequestData("GET", "http://localhost/api/swagger.xyz");

        // Act
        var response = await _function.RenderOpenApiDocument(request, "xyz");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Type").First().ShouldBe("application/json"); // Default to JSON
    }
}