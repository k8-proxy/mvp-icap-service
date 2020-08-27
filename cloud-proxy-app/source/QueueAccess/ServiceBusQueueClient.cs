using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp.QueueAccess
{
    public class ServiceBusQueueClient : IServiceQueueClient
    {
        private readonly ICloudConfiguration _cloudConfiguration;
        private readonly QueueClient _queueClient;
        private readonly MessageHandlerOptions _messageHandlerOptions;

        public ServiceBusQueueClient(ICloudConfiguration cloudConfiguration)
        {
            _cloudConfiguration = cloudConfiguration;
            _queueClient = new QueueClient(_cloudConfiguration.TransactionOutcomeQueueConnectionString, _cloudConfiguration.TransactionOutcomeQueueName);
            _messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
        }

        public IQueueListener Register(string messageType, string identifier)
        {
            var messageQueue = new BlockingCollection<Message>();
            _queueClient.RegisterMessageHandler((Message msg, CancellationToken ct) =>
            {
            if ((msg.Label == messageType) && (msg.UserProperties["file-id"] as string == identifier))
                {
                    messageQueue.Add(msg, ct);
                    messageQueue.CompleteAdding();
                }
                return Task.CompletedTask;
            }, _messageHandlerOptions);

            return new ServiceBusQueueListener(_queueClient, messageQueue);
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }
    }
}
