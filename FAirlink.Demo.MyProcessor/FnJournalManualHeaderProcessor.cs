using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FJarvis.Data;
using FJarvis.Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FAirlink.Demo.MyProcessor
{
    public static class FnJournalManualHeaderProcessor
    {
        [FunctionName("FnJournalManualHeaderProcessor")]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            var state = false;
            
            var clientOptions = new ServiceBusClientOptions();
            clientOptions.TransportType = ServiceBusTransportType.AmqpWebSockets;

            var client = new ServiceBusClient("Endpoint=sb://fctg-airlink-demo.servicebus.windows.net/;SharedAccessKeyName=airlink-demo-bus-sas;SharedAccessKey=8XaO2YGOVhfeGUStKRPqc/AnylwNMY/W++ASbEWIUPY=;EntityPath=jarvisdemo",clientOptions);
            var receiver = client.CreateReceiver("jarvisdemo", new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
            });
            
            ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
            
            if (message != null)
            {
                // Process the message
                Console.WriteLine($"Received message: {message.Body}");

                var headers = JsonConvert.DeserializeObject<HashSet<HeaderInfo>>(message.Body.ToString());
               
                // Header Logic Here
                if (headers.Any(e=>e.Bitmask.Equals("5188146770730811392;")) || headers.HasTrait<Flight>())
                {
                    Console.WriteLine($"Message Passed!");    
                    await receiver.CompleteMessageAsync(message);
                    state = true;
                }
                else
                {
                    Console.WriteLine($"Message Failed!");
                    await receiver.AbandonMessageAsync(message);
                }
            }


            return new OkObjectResult(state);
            
        }
    }
}