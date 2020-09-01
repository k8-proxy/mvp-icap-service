using Microsoft.Azure.ServiceBus;
using System.Collections.Concurrent;
using System.Threading;

namespace Glasswall.IcapServer.CloudProxyApp.QueueAccess
{
    public class ServiceBusQueueListener : IQueueListener
    {
        private readonly IQueueClient _queueClient;
        private readonly BlockingCollection<Message> _messageQueue;

        public ServiceBusQueueListener(IQueueClient queueClient, BlockingCollection<Message> messageQueue)
        {
            _queueClient = queueClient;
            _messageQueue = messageQueue;
        }

        public Message Take(CancellationToken cancellationToken)
        {
            var message = _messageQueue.Take(cancellationToken);
            _queueClient.CompleteAsync(message.SystemProperties.LockToken);
            return message;
        }
    }
}
