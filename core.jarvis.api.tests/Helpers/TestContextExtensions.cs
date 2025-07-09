using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>
/// Extension methods for test contexts
/// </summary>
public static class TestContextExtensions
{
    /// <summary>
    /// Gets HttpRequestData from test context
    /// </summary>
    public static Task<HttpRequestData?> GetHttpRequestDataAsync(this FunctionContext context)
    {
        if (context.Items.TryGetValue("HttpRequestData", out var requestData))
        {
            return Task.FromResult(requestData as HttpRequestData);
        }
        return Task.FromResult<HttpRequestData?>(null);
    }
}