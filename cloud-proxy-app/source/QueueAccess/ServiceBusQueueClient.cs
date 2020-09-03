using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp.QueueAccess
{
    public class ServiceBusQueueClient : IServiceQueueClient
    {
        private readonly ICloudConfiguration _cloudConfiguration;
        private readonly ILogger<ServiceBusQueueClient> _logger;
        private readonly IQueueClient _queueClient;
        private readonly MessageHandlerOptions _messageHandlerOptions;

        public ServiceBusQueueClient(ICloudConfiguration cloudConfiguration, Func<string, string, IQueueClient> queueClientFactory, ILogger<ServiceBusQueueClient> logger)
        {
            _cloudConfiguration = cloudConfiguration;
            _logger = logger;
            _queueClient = queueClientFactory(_cloudConfiguration.TransactionOutcomeQueueConnectionString, _cloudConfiguration.TransactionOutcomeQueueName);
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

        Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            _logger.LogError(exceptionReceivedEventArgs.Exception, $"{_cloudConfiguration.TransactionOutcomeQueueName} Message handler encountered an exception");
            return Task.CompletedTask;
        }
    }
}
