using Microsoft.Azure.ServiceBus;
using System.Threading;

namespace Glasswall.IcapServer.CloudProxyApp.QueueAccess
{
    public interface IQueueListener
    {
        public Message Take(CancellationToken cancellationToken);
    }
}
