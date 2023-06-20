using Azure.Messaging.ServiceBus;
using FJarvis.Data.Data;
using Newtonsoft.Json;

namespace FJarvis.Data
{
    public class ServiceBus
    {
        private static ServiceBusClient client;
        private static ServiceBusSender sender;
        private static ServiceBusProcessor processor;
        private static ServiceBusReceiver receiver;

        private static Dictionary<string, Func<ServiceBusReceivedMessage, Task>> _subscriptions = new Dictionary<string, Func<ServiceBusReceivedMessage, Task>>();

        public ServiceBus(JournalLogger journalLogger)
        {
            var clientOptions = new ServiceBusClientOptions();
            clientOptions.TransportType = ServiceBusTransportType.AmqpWebSockets;
            
            var client = new ServiceBusClient("Endpoint=sb://fctg-airlink-demo.servicebus.windows.net/;SharedAccessKeyName=airlink-demo-bus-sas;SharedAccessKey=8XaO2YGOVhfeGUStKRPqc/AnylwNMY/W++ASbEWIUPY=;EntityPath=jarvisdemo",clientOptions);
            receiver = client.CreateReceiver("jarvisdemo", new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
            });
        }
        
        public async Task ProcessMessagesAsync()
        {
            ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
            var headers = JsonConvert.DeserializeObject<HashSet<HeaderInfo>>(message.Body.ToString());
            foreach (var header in headers)
            {
                if (_subscriptions.ContainsKey(header.Bitmask))
                {
                    await _subscriptions[header.Bitmask](message);
                }
            }
            

        }


        public async Task Subscribe(EntityQueryDesc criteria, Func<ServiceBusReceivedMessage, Task> myHandler)
        {
            // Convert Criteria to a Compressed Bitmask.
            var criteriaBitmask = criteria.ToBitmask();
            
        }
    }
}