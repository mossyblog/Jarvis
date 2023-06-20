// See https://aka.ms/new-console-template for more information

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Diagnostics;
using FJarvis.Data;
using FJarvis.Data.Data;
using FJarvis.Data.Traits;
using FJarvis.Traits.FlightCentre;
using Newtonsoft.Json;

namespace FAirlink.Demo.ClientDemo
{


    public class Program
    {
        private static ServiceBusClient client;
        private static ServiceBusSender sender;
        private static ServiceBusProcessor processor;
        private static ServiceBusReceiver receiver;

        // number of messages to be sent to the queue
        const int numOfMessages = 3;
        
        static async Task Main(string[] args)
        {
            var clientOptions = new ServiceBusClientOptions
            { 
                TransportType = ServiceBusTransportType.AmqpWebSockets
            };
            //client = new ServiceBusClient("FCTG-Airlink-Demo.servicebus.windows.net", new DefaultAzureCredential(), clientOptions);
            //sender = client.CreateSender("jarvisdemo");

            client = new ServiceBusClient("Endpoint=sb://fctg-airlink-demo.servicebus.windows.net/;SharedAccessKeyName=airlink-demo-bus-sas;SharedAccessKey=8XaO2YGOVhfeGUStKRPqc/AnylwNMY/W++ASbEWIUPY=;EntityPath=jarvisdemo",clientOptions);
            sender = client.CreateSender("jarvisdemo");

            // create a processor that we can use to process the messages
            // TODO: Replace the <QUEUE-NAME> placeholder
            var opts = new ServiceBusProcessorOptions();
            opts.ReceiveMode = ServiceBusReceiveMode.PeekLock;

            processor = client.CreateProcessor("jarvisdemo", opts);
            receiver = client.CreateReceiver("jarvisdemo");

            // add handler to process messages
            processor.ProcessMessageAsync += MessageHandler;

            // add handler to process any errors
            processor.ProcessErrorAsync += ErrorHandler;
            

            var queueName = "jarvisdemo";
            // number of messages to be sent to the queue
            const int numOfMessages = 3;
            
            // create a batch 
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
            
            for (int i = 1; i <= numOfMessages; i++)
            {

                Jarvis.Initialize();
                
                /*
                var subMatchCriteria = new EntityQueryDesc
                {
                    All = new TraitType[]
                    {
                        TraitType.ReadWrite<Coupon>(),
                        TraitType.ReadWrite<Flight>(), 
                    }
                };*/
                
                
                //Jarvis.ServiceBus().InvokeMySpecialMessage();
                //Jarvis.ServiceBus().Subscribe(subMatchCriteria, myHandler);
                
                var entityA = Jarvis.EntityManager().CreatEntity();
                var journal = Jarvis.Journal();
                var entityManager = Jarvis.EntityManager();
        
                var firstEntity = entityManager.CreatEntity();
                var secondEntity = entityManager.CreatEntity();

                var flight = new Flight();
                var coupon = new Coupon();
                
                Jarvis.EntityManager().AddTraitData(firstEntity, flight);
                Jarvis.EntityManager().AddTraitData(secondEntity, flight, coupon);

                var headers = journal.Headers();
                var message = JsonConvert.SerializeObject(headers);
                
                // try adding a message to the batch
                if (!messageBatch.TryAddMessage(new ServiceBusMessage(message)))
                {
                    // if it is too large for the batch
                    throw new Exception($"The message {i} is too large to fit in the batch.");
                }
            }
            
            try
            {
                // Use the producer client to send the batch of messages to the Service Bus queue
                await sender.SendMessagesAsync(messageBatch);
                Console.WriteLine($"A batch of {numOfMessages} messages has been published to the queue.");


                // start processing 
                await processor.StartProcessingAsync();
                Console.WriteLine("Wait for a minute and then press any key to end the processing");
                Console.ReadKey();

                // stop processing 
                Console.WriteLine("\nStopping the receiver...");
                await processor.StopProcessingAsync();
                Console.WriteLine("Stopped receiving messages");
                
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
               // await sender.DisposeAsync();
                //await client.DisposeAsync();
            }

       



        }
        
        static async Task myHandler(ServiceBusReceivedMessage message)
        {
        }

        // handle received messages
        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            var headers = JsonConvert.DeserializeObject<HashSet<HeaderInfo>>(body);

            foreach (var header in headers)
            {
                Console.WriteLine($"Received Headers: {header.EntityId } | {header.Bitmask}");
                header.HasTrait<Flight>();
                
                // Invoke My next .. logicapp.. it ?????
            }
            
            
            // Header Logic Here
            if (headers.Any(e=>e.Bitmask.Equals("5188146770730811392;")))
            {
                Console.WriteLine($"Message Passed!");    
                await args.CompleteMessageAsync(args.Message);
            }
            else
            {
                Console.WriteLine($"Message Failed!");
                await args.AbandonMessageAsync(args.Message); // --> GOES TO DEAD LETTER QUEUE.
            }





        }

        // handle any errors when receiving messages
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
    }
}