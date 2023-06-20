using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FJarvis.Data;
using FJarvis.Data.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FAirlink.Demo.MyProcessor
{
    public class FnJournalHeaderProcessor
    {
        private readonly HttpClient _httpClient;

        public FnJournalHeaderProcessor(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }
        
        [FunctionName("FnJournalHeaderProcessor")]
        public async Task RunAsync([ServiceBusTrigger("jarvisdemo", Connection = "QueueConnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            // Create the HTTP request content
            var requestBody = new { message = myQueueItem };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            // Send the HTTP request to the Logic App
            var response = await _httpClient.PostAsync("https://fctg-airlink-demo.azurewebsites.net/", content);
            response.EnsureSuccessStatusCode();

            log.LogInformation($"Invoking Service Bus POST XYZ");



        }
    }
}
