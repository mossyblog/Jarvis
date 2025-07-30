using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace core.jarvis.api.Functions
{
    public class HealthFunction
    {
        private readonly ILogger<HealthFunction> _logger;

        public HealthFunction(ILogger<HealthFunction> logger)
        {
            _logger = logger;
        }

        [Function("Health")]
        public async Task<HttpResponseData> Health(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("Health check endpoint called");

            var response = req.CreateResponse(HttpStatusCode.OK);
            
            await response.WriteAsJsonAsync(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "core.jarvis.api"
            });

            return response;
        }
    }
}