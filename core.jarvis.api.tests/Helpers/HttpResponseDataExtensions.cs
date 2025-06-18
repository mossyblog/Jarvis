using System.Text;
using Microsoft.Azure.Functions.Worker.Http;

namespace core.jarvis.api.tests.Integration.Functions;

/// <summary>
/// Extension methods for HttpResponseData to support test scenarios.
/// </summary>
public static class HttpResponseDataExtensions
{
    public static async Task WriteStringAsync(this HttpResponseData response, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await response.Body.WriteAsync(bytes, 0, bytes.Length);
        response.Body.Position = 0; // Reset position for reading
    }
}