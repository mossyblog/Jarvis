using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace core.jarvis.api.tests.Helpers;

/// <summary>
/// Factory for creating test objects.
/// </summary>
public static class TestFactory
{
    /// <summary>
    /// Creates a mock HttpRequestData for testing.
    /// </summary>
    public static HttpRequestData CreateHttpRequestData(string method, string url, string? body = null)
    {
        var context = new Mock<FunctionContext>();
        var serviceProvider = new Mock<IServiceProvider>();
        var services = new ServiceCollection();
        
        context.Setup(c => c.InstanceServices).Returns(serviceProvider.Object);
        
        var request = new MockHttpRequestData(context.Object, new Uri(url), method);
        
        if (body != null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            request.SetBody(new MemoryStream(bytes));
        }
        
        return request;
    }

    /// <summary>
    /// Gets the response body as a string.
    /// </summary>
    public static async Task<string> GetResponseBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
}

/// <summary>
/// Mock implementation of HttpRequestData for testing.
/// </summary>
public class MockHttpRequestData : HttpRequestData
{
    private readonly string _method;
    private readonly Uri _url;
    private HttpHeadersCollection? _headers;
    private Stream _body;

    public MockHttpRequestData(FunctionContext functionContext, Uri url, string method) 
        : base(functionContext)
    {
        _url = url;
        _method = method;
        _headers = new HttpHeadersCollection();
        _body = new MemoryStream();
    }

    public override Stream Body => _body;

    public void SetBody(Stream body)
    {
        _body = body;
    }

    public override HttpHeadersCollection Headers => _headers ??= new HttpHeadersCollection();

    public override IReadOnlyCollection<IHttpCookie> Cookies => new List<IHttpCookie>();

    public override Uri Url => _url;

    public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();

    public override string Method => _method;

    public override HttpResponseData CreateResponse()
    {
        return new MockHttpResponseData(FunctionContext);
    }
}

/// <summary>
/// Mock implementation of HttpResponseData for testing.
/// </summary>
public class MockHttpResponseData : HttpResponseData
{
    private HttpHeadersCollection _headers;

    public MockHttpResponseData(FunctionContext functionContext) 
        : base(functionContext)
    {
        Body = new MemoryStream();
        StatusCode = HttpStatusCode.OK;
        _headers = new HttpHeadersCollection();
    }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers 
    { 
        get => _headers;
        set => _headers = value;
    }

    public override Stream Body { get; set; }

    public override HttpCookies Cookies => new MockHttpCookies();
}

/// <summary>
/// Mock implementation of HttpCookies for testing.
/// </summary>
public class MockHttpCookies : HttpCookies
{
    private readonly List<IHttpCookie> _cookies = new();

    public override void Append(IHttpCookie cookie)
    {
        _cookies.Add(cookie);
    }

    public override void Append(string name, string value)
    {
        // Simple implementation for testing
    }

    public override IHttpCookie CreateNew()
    {
        return new MockHttpCookie();
    }
}

/// <summary>
/// Mock implementation of IHttpCookie for testing.
/// </summary>
public class MockHttpCookie : IHttpCookie
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public bool? HttpOnly { get; set; }
    public double? MaxAge { get; set; }
    public string? Path { get; set; }
    public SameSite SameSite { get; set; }
    public bool? Secure { get; set; }
}